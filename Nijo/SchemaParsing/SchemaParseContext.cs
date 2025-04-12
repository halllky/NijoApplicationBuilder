using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using Nijo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Nijo.SchemaParsing;

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
            new ValueObjectModel(),
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

        Document = xDocument;
        Models = models.ToDictionary(m => m.SchemaName);
        _valueMemberTypes = valueMemberTypes.ToDictionary(m => m.SchemaTypeName);
        _nodeOptions = nodeOptions.ToDictionary(o => o.AttributeName);
    }

    public XDocument Document { get; }
    public IReadOnlyDictionary<string, IModel> Models { get; }
    private readonly IReadOnlyDictionary<string, IValueMemberType> _valueMemberTypes;
    private readonly IReadOnlyDictionary<string, NodeOption> _nodeOptions;

    private const string ATTR_IS = "is";
    internal const string ATTR_NODE_TYPE = "Type";

    internal const string NODE_TYPE_CHILD = "child";
    internal const string NODE_TYPE_CHILDREN = "children";
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
        // ルート集約, Child, Children
        if (TryGetAggregateNodeType(xElement, out var aggregateNodeType)) {
            return aggregateNodeType.Value;
        }

        // 親がenumなら静的区分の値
        if (xElement.Parent != null
         && xElement.Parent.Parent == Document.Root
         && xElement.Parent.Attribute(ATTR_NODE_TYPE)?.Value == EnumDefParser.SCHEMA_NAME) {
            return E_NodeType.StaticEnumValue;
        }

        // 以降はType属性の値で区別
        var type = xElement.Attribute(ATTR_NODE_TYPE);
        if (type == null) {
            return E_NodeType.Unknown;
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
    /// <summary>
    /// <see cref="GetNodeType(XElement)"/> のロジックのうちルート集約・Child・Childrenの判定には
    /// <see cref="SchemaParseContext"/> のインスタンスが要らないのでその部分だけ切り出したもの
    /// </summary>
    internal static bool TryGetAggregateNodeType(XElement xElement, [NotNullWhen(true)] out E_NodeType? nodeType) {

        // ルート要素直下に定義されている場合はルート集約
        if (xElement.Parent == xElement.Document?.Root) {
            nodeType = E_NodeType.RootAggregate;
            return true;
        }
        // Child
        var type = xElement.Attribute(ATTR_NODE_TYPE);
        if (type?.Value == NODE_TYPE_CHILD) {
            nodeType = E_NodeType.ChildAggregate;
            return true;
        }
        // Children
        if (type?.Value == NODE_TYPE_CHILDREN) {
            nodeType = E_NodeType.ChildrenAggregate;
            return true;
        }

        nodeType = null;
        return false;
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
        return Document.Root
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
        return Document.XPathSelectElement(xPath);
    }
    /// <summary>
    /// 引数の集約を参照している集約を探して返します。
    /// </summary>
    internal IEnumerable<XElement> FindRefFrom(XElement xElement) {
        // まずパフォーマンスのためXPathで高速に絞り込む
        var physicalName = GetPhysicalName(xElement);
        return Document.XPathSelectElements($"//*[@{ATTR_NODE_TYPE}='{NODE_TYPE_REFTO}:{physicalName}']") ?? [];
    }
    #endregion RefTo


    #region 検証
    /// <summary>
    /// XMLドキュメントがスキーマ定義として不正な状態を持たないかを検証し、
    /// 検証に成功した場合はアプリケーションスキーマのインスタンスを返します。
    /// </summary>
    /// <param name="xDocument">XMLドキュメント</param>
    /// <param name="schema">作成完了後のスキーマ</param>
    /// <param name="logger">エラーがある場合はここにその内容が表示される</param>
    /// <returns>スキーマの作成に成功したかどうか</returns>
    public bool TryBuildSchema(XDocument xDocument, out ApplicationSchema schema, ILogger logger) {
        schema = new ApplicationSchema(xDocument, this);
        var errorsList = new List<(XElement, string)>();

        foreach (var el in xDocument.Root?.Descendants() ?? []) {

            var nodeType = GetNodeType(el);
            var typeAttrValue = el.Attribute(ATTR_NODE_TYPE)?.Value ?? string.Empty;

            // ノードの種類に基づくチェック
            switch (nodeType) {
                // ノードの種類が不明な場合
                case E_NodeType.Unknown:
                    if (string.IsNullOrEmpty(typeAttrValue)) {
                        errorsList.Add((el, $"ノードの種類が不明です。{ATTR_NODE_TYPE}属性が指定されているか確認してください。"));
                    } else {
                        errorsList.Add((el, $"ノードの種類 '{typeAttrValue}' は有効ではありません。"));
                    }
                    break;

                // ルート集約の場合
                case E_NodeType.RootAggregate:
                    var model = Models.GetValueOrDefault(typeAttrValue);
                    if (model == null) {
                        errorsList.Add((el, $"{ATTR_NODE_TYPE}属性でモデルが指定されていません。使用できる値は {Models.Keys.Join(", ")} です。"));
                    } else {
                        // モデル単位の検証
                        model.Validate(el, this, (el, err) => errorsList.Add((el, err)));
                    }
                    break;

                // Child
                case E_NodeType.ChildAggregate:
                    break;

                // Children
                case E_NodeType.ChildrenAggregate:
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
                    AddError = err => errorsList.Add((el, err)),
                    SchemaParseContext = this,
                });
            }
        }

        // エラー内容表示
        if (errorsList.Count > 0) {
            var errors = errorsList
                .GroupBy(x => x.Item1)
                .Select(x => new ValidationError {
                    XElement = x.Key,
                    Errors = x.Select(y => y.Item2).ToArray(),
                });
            var logBuilder = new StringBuilder();
            foreach (var err in errors) {
                var path = err.XElement
                    .AncestorsAndSelf()
                    .Reverse()
                    .Skip(1)
                    .Select(el => el.Name.LocalName)
                    .Join("/");

                logBuilder.AppendLine(path);
                foreach (var msg in err.Errors) {
                    logBuilder.AppendLine($"  - {WithIndent(msg, "    ")}");
                }
                logBuilder.AppendLine();
            }
            logger.LogError(logBuilder.ToString());
        }

        return errorsList.Count == 0;
    }
    public class ValidationError {
        public required XElement XElement { get; init; }
        public required IReadOnlyCollection<string> Errors { get; init; }

        public override string ToString() {
            return $"{XElement.AncestorsAndSelf().Reverse().Select(el => el.Name.LocalName).Join("/")}: {Errors.Join(", ")}";
        }
    }
    #endregion 検証


    /// <summary>
    /// このスキーマ内で定義されている文字種を列挙する
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<string> GetCharacterTypes() {
        return Document.XPathSelectElements($"//*[@{BasicNodeOptions.CharacterType.AttributeName}]")
            .Select(el => el.Attribute(BasicNodeOptions.CharacterType.AttributeName)?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrEmpty(value))
            .Distinct()
            .OrderBy(charType => charType);
    }
}
