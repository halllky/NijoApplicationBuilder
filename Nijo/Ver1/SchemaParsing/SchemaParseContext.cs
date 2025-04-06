using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Nijo.Ver1.SchemaParsing;

/// <summary>
/// XML要素をこのアプリケーションのルールに従って解釈する
/// </summary>
public class SchemaParseContext {

    /// <summary>
    /// 既定の型解釈ルールでコンテキストを作成します。
    /// </summary>
    /// <returns></returns>
    public static SchemaParseContext Default(XDocument xDocument) {
        var models = new IModel[] {
            new DataModel(),
            new QueryModel(),
            new CommandModel(),
            new StaticEnumModel(),
        };
        var valueMemberTypes = new IValueMemberType[] {
            new ValueMemberTypes.Word(),
            new ValueMemberTypes.IntMember(),
            new ValueMemberTypes.DateTimeMember(),
            new ValueMemberTypes.Description(),
        };
        var nodeOptions = new NodeOption[] {
            BasicNodeOptions.DisplayName,
            BasicNodeOptions.DbName,
            BasicNodeOptions.LatinName,
            BasicNodeOptions.IsKey,
            BasicNodeOptions.IsRequired,
            BasicNodeOptions.GenerateDefaultQueryModel,
            BasicNodeOptions.GenerateBatchUpdateCommand,
            BasicNodeOptions.IsReadOnly,
            BasicNodeOptions.HasLifeCycle,
            BasicNodeOptions.MaxLength,
            BasicNodeOptions.CharacterType,
            BasicNodeOptions.TotalDigit,
            BasicNodeOptions.DecimalPlace,
            BasicNodeOptions.SequenceName,
        };
        return new SchemaParseContext(xDocument, models, valueMemberTypes, nodeOptions);
    }

    private SchemaParseContext(XDocument xDocument, IModel[] models, IValueMemberType[] valueMemberTypes, NodeOption[] nodeOptions) {
        // スキーマ定義名重複チェック
        var appearedName = new HashSet<string>();
        var duplicates = new HashSet<string>();
        foreach (var name in models.Select(m => m.SchemaName).Concat(valueMemberTypes.Select(t => t.SchemaTypeName))) {
            if (appearedName.Contains(name)) {
                duplicates.Add(name);
            } else {
                appearedName.Add(name);
            }
        }
        if (duplicates.Count > 0) {
            throw new InvalidOperationException($"型名 {string.Join(", ", duplicates)} が重複しています。");
        }

        // オプション属性のキー重複チェック
        var groupedOptions = nodeOptions
            .GroupBy(opt => opt.AttributeName)
            .Where(group => group.Count() >= 2)
            .ToArray();
        if (groupedOptions.Length > 0) {
            throw new InvalidOperationException($"オプション属性名 {groupedOptions.Select(g => g.Key).Join(", ")} が重複しています。");
        }

        // 予約語
        if (nodeOptions.Any(opt => opt.AttributeName == ATTR_NODE_TYPE)) {
            throw new InvalidOperationException($"{ATTR_NODE_TYPE} という名前のオプション属性は定義できません。");
        }

        _xDocument = xDocument;
        Models = models.ToDictionary(m => m.SchemaName);
        _valueMemberTypes = valueMemberTypes.ToDictionary(m => m.SchemaTypeName);
        _nodeOptions = nodeOptions.ToDictionary(o => o.AttributeName);
    }

    private readonly XDocument _xDocument;
    public IReadOnlyDictionary<string, IModel> Models { get; }
    private readonly IReadOnlyDictionary<string, IValueMemberType> _valueMemberTypes;
    private readonly IReadOnlyDictionary<string, NodeOption> _nodeOptions;

    private const string ATTR_IS = "is";
    private const string ATTR_NODE_TYPE = "Type";

    private const string NODE_TYPE_CHILD = "child";
    private const string NODE_TYPE_CHILDREN = "children";
    private const string NODE_TYPE_REFTO = "ref-to";

    /// <summary>
    /// 物理名
    /// </summary>
    internal string GetPhysicalName(XElement xElement) {
        return xElement.Name.LocalName;
    }
    /// <summary>
    /// 表示名称
    /// </summary>
    internal string GetDisplayName(XElement xElement) {
        return xElement.Attribute(BasicNodeOptions.DisplayName.AttributeName)?.Value ?? xElement.Name.LocalName;
    }
    /// <summary>
    /// DB名
    /// </summary>
    internal string GetDbName(XElement xElement) {
        return xElement.Attribute(BasicNodeOptions.DbName.AttributeName)?.Value ?? xElement.Name.LocalName;
    }
    /// <summary>
    /// ラテン名
    /// </summary>
    internal string GetLatinName(XElement xElement) {
        return xElement.Attribute(BasicNodeOptions.LatinName.AttributeName)?.Value ?? xElement.Name.LocalName.ToHashedString();
    }


    /// <summary>
    /// XML要素の種類を判定する。
    /// 編集途中の不正な状態であっても入力内容検査等に使う必要があるため、
    /// XML要素が不正であっても例外を出さない。
    /// </summary>
    internal E_NodeType GetNodeType(XElement xElement) {
        // ルート要素直下に定義されている場合はルート集約
        if (xElement.Parent == _xDocument.Root) {
            return E_NodeType.RootAggregate;
        }
        // 親がenumなら静的区分の値
        if (xElement.Parent != null
         && xElement.Parent.Parent == _xDocument.Root
         && xElement.Parent.Attribute(ATTR_NODE_TYPE)?.Value == EnumDefParser.SCHEMA_NAME) {
            return E_NodeType.StaticEnumValue;
        }

        // 以降はType属性の値で区別
        var type = xElement.Attribute(ATTR_NODE_TYPE);
        if (type == null) {
            return E_NodeType.Unknown;
        }

        // Child
        if (type.Value == NODE_TYPE_CHILD) {
            return E_NodeType.ChildAggregate;
        }
        // Children
        if (type.Value == NODE_TYPE_CHILDREN) {
            return E_NodeType.ChildrenAggregate;
        }
        // RefTo
        if (type.Value.StartsWith(NODE_TYPE_REFTO)) {
            return E_NodeType.Ref;
        }
        // ValueMember
        if (TryResolveMemberType(xElement, out _)) {
            return E_NodeType.ValueMember;
        }
        return E_NodeType.Unknown;
    }


    #region オプション属性
    private const string IS_DYNAMIC_ENUM_MODEL = "dynamic-enum";
    /// <summary>
    /// XML要素に定義されているオプション属性を返します。
    /// </summary>
    internal IEnumerable<NodeOption> GetOptions(XElement xElement) {
        var attrs = xElement.Attributes().Select(attr => attr.Name.LocalName).ToHashSet();
        return _nodeOptions.Values.Where(opt => attrs.Contains(opt.AttributeName));
    }
    #endregion オプション属性


    #region Aggregate
    /// <summary>
    /// XML要素と対応するモデルを返します。特定できなかった場合は例外。
    /// </summary>
    internal IModel GetModel(XElement xElement) {
        return Models[xElement.Attribute(ATTR_NODE_TYPE)?.Value ?? throw new InvalidOperationException()];
    }
    /// <summary>
    /// ルート集約や子集約を表すXML要素を <see cref="AggregateBase"/> のインスタンスに変換します。
    /// XML要素が集約を表すもので無かった場合は例外を送出します。
    /// </summary>
    internal AggregateBase ToAggregateBase(XElement xElement, ISchemaPathNode? previous) {
        var nodeType = GetNodeType(xElement);
        if (nodeType == E_NodeType.RootAggregate) {
            return new RootAggregate(xElement, this, previous);
        }
        if (nodeType == E_NodeType.ChildAggregate) {
            return new ChildAggreagte(xElement, this, previous);
        }
        if (nodeType == E_NodeType.ChildrenAggregate) {
            return new ChildrenAggreagte(xElement, this, previous);
        }
        throw new InvalidOperationException($"集約ではありません: {xElement}");
    }
    #endregion Aggregate


    #region ValueMember
    /// <summary>
    /// このスキーマで定義されている静的区分の種類を返します。
    /// </summary>
    /// <returns></returns>
    private IReadOnlyDictionary<string, ValueMemberTypes.StaticEnumMember> GetStaticEnumMembers() {
        return _xDocument.Root
            ?.Elements()
            .Where(el => el.Attribute(ATTR_NODE_TYPE)?.Value == EnumDefParser.SCHEMA_NAME)
            .ToDictionary(GetPhysicalName, el => new ValueMemberTypes.StaticEnumMember(el, this))
            ?? [];
    }
    /// <summary>
    /// スキーマ解釈ルールとしてあらかじめ定められた値種別および静的区分の種類の一覧を返します。
    /// </summary>
    public IEnumerable<IValueMemberType> GetValueMemberTypes() {
        // 単語型など予め登録された型
        foreach (var type in _valueMemberTypes.Values) {
            yield return type;
        }
        // 列挙体
        foreach (var type in GetStaticEnumMembers().Values) {
            yield return type;
        }
    }
    /// <summary>
    /// ValueMemberを表すXML要素の種別（日付, 数値, ...等）を判別して返します。
    /// </summary>
    internal bool TryResolveMemberType(XElement xElement, out IValueMemberType valueMemberType) {
        var type = xElement.Attribute(ATTR_NODE_TYPE);
        if (type == null) {
            valueMemberType = null!;
            return false;
        }

        // 単語型など予め登録された型
        if (_valueMemberTypes.TryGetValue(type.Value, out var vmType)) {
            valueMemberType = vmType;
            return true;
        }

        // 列挙体
        if (GetStaticEnumMembers().TryGetValue(type.Value, out var enumMember)) {
            valueMemberType = enumMember;
            return true;
        }

        // 解決できなかった
        valueMemberType = null!;
        return false;
    }
    #endregion ValueMember


    #region RefTo
    /// <summary>
    /// 参照先のXML要素を返します。
    /// </summary>
    internal XElement? FindRefTo(XElement xElement) {
        var type = xElement.Attribute(ATTR_NODE_TYPE) ?? throw new InvalidOperationException();
        var xPath = $"//{type.Value.Split(':')[1]}";
        return _xDocument.XPathSelectElement(xPath);
    }
    /// <summary>
    /// 引数の集約を参照している集約を探して返します。
    /// </summary>
    internal IEnumerable<XElement> FindRefFrom(XElement xElement) {
        // まずパフォーマンスのためXPathで高速に絞り込む
        var physicalName = GetPhysicalName(xElement);
        return _xDocument.XPathSelectElements($"//*[@{ATTR_NODE_TYPE}='{NODE_TYPE_REFTO}:{physicalName}']") ?? [];
    }
    #endregion RefTo


    #region 検証
    /// <summary>
    /// XMLドキュメントがスキーマ定義として不正な状態を持たないかを検証し、
    /// 検証に成功した場合はアプリケーションスキーマのインスタンスを返します。
    /// </summary>
    /// <param name="xDocument">XMLドキュメント</param>
    /// <param name="schema">作成完了後のスキーマ</param>
    /// <param name="errors">エラーがある場合はここにその内容が格納される</param>
    /// <returns>スキーマの作成に成功したかどうか</returns>
    public bool TryBuildSchema(XDocument xDocument, out ApplicationSchema schema, out ICollection<ValidationError> errors) {
        schema = new ApplicationSchema(xDocument, this);
        var errorsList = new List<ValidationError>();

        foreach (var el in xDocument.Root?.Descendants() ?? []) {
            var thisErrors = new List<string>();
            errorsList.Add(new() { XElement = el, Errors = thisErrors });

            var nodeType = GetNodeType(el);
            var typeAttrValue = el.Attribute(ATTR_NODE_TYPE)?.Value ?? string.Empty;

            // ノードの種類に基づくチェック
            switch (nodeType) {
                // ノードの種類が不明な場合
                case E_NodeType.Unknown:
                    thisErrors.Add($"ノードの種類が不明です。{ATTR_NODE_TYPE}属性が指定されているか確認してください。");
                    break;

                // ルート集約の場合
                case E_NodeType.RootAggregate:
                    // モデルがあるか
                    if (!Models.ContainsKey(typeAttrValue)) {
                        thisErrors.Add($"{ATTR_NODE_TYPE}属性でモデルが指定されていません。使用できる値は {Models.Keys.Join(", ")} です。");
                    }

                    // メンバーにIsKey属性がついているかチェック
                    if (!HasAnyMemberWithIsKeyAttribute(el)) {
                        thisErrors.Add("ルート集約のメンバーのいずれにもIsKey属性がついていません。少なくとも1つのメンバーにIsKey属性を設定してください。");
                    }
                    break;

                // Child
                case E_NodeType.ChildAggregate:
                    break;

                // Children
                case E_NodeType.ChildrenAggregate:
                    // メンバーにIsKey属性がついているかチェック
                    if (!HasAnyMemberWithIsKeyAttribute(el)) {
                        thisErrors.Add("Children集約のメンバーのいずれにもIsKey属性がついていません。少なくとも1つのメンバーにIsKey属性を設定してください。");
                    }
                    break;

                case E_NodeType.ValueMember:
                    break;

                case E_NodeType.Ref:
                    break;

                case E_NodeType.StaticEnumType:
                    break;

                case E_NodeType.StaticEnumValue:
                    break;

                default:
                    break;
            }

            // オプション属性に基づくチェック
            foreach (var opt in GetOptions(el)) {
                opt.Validate(new() {
                    Value = el.Attribute(opt.AttributeName)!.Value,
                    XElement = el,
                    NodeType = nodeType,
                    Errors = thisErrors,
                    SchemaParseContext = this,
                });
            }
        }

        errors = errorsList
            .Where(e => e.Errors.Count > 0)
            .ToArray();

        return errors.Count == 0;
    }
    public class ValidationError {
        public required XElement XElement { get; init; }
        public required IReadOnlyCollection<string> Errors { get; init; }

        public override string ToString() {
            return $"{XElement.AncestorsAndSelf().Reverse().Select(el => el.Name.LocalName).Join("/")}: {Errors.Join(", ")}";
        }
    }

    /// <summary>
    /// 指定されたXML要素のメンバー（子要素）のいずれかにIsKey属性がついているかをチェックします。
    /// </summary>
    /// <param name="xElement">チェック対象のXML要素</param>
    /// <returns>IsKey属性がついているメンバーが存在する場合はtrue、それ以外はfalse</returns>
    private bool HasAnyMemberWithIsKeyAttribute(XElement xElement) {
        // 子要素を取得
        var children = xElement.Elements();

        // いずれかの子要素にIsKey属性がついているかチェック
        foreach (var child in children) {
            var isKeyAttr = child.Attribute(BasicNodeOptions.IsKey.AttributeName);
            if (isKeyAttr != null && bool.TryParse(isKeyAttr.Value, out bool isKey) && isKey) {
                return true;
            }
        }

        return false;
    }
    #endregion 検証
}
