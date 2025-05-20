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
    public SchemaParseContext(XDocument xDocument, SchemaParseRule rule) {
        // ルールの検証
        rule.ThrowIfInvalid();

        Document = xDocument;
        Models = rule.Models.ToDictionary(m => m.SchemaName);
        _rule = rule;
        _valueMemberTypes = rule.ValueMemberTypes.ToDictionary(m => m.SchemaTypeName);
    }

    public XDocument Document { get; }
    public IReadOnlyDictionary<string, IModel> Models { get; }
    private readonly SchemaParseRule _rule;
    private readonly IReadOnlyDictionary<string, IValueMemberType> _valueMemberTypes;
    /// <summary>enum, value-object を除いた値型の一覧</summary>
    public IEnumerable<IValueMemberType> ValueMemberTypes => _rule.ValueMemberTypes;

    private const string ATTR_IS = "is";
    internal const string ATTR_NODE_TYPE = "Type";

    internal const string NODE_TYPE_CHILD = "child";
    internal const string NODE_TYPE_CHILDREN = "children";
    internal const string NODE_TYPE_REFTO = "ref-to";

    /// <summary>
    /// 物理名。スキーマ内での物理名の衝突を考慮した値を返す。
    /// </summary>
    internal string GetPhysicalName(XElement xElement) {
        var nodeType = GetNodeType(xElement);

        // ルート集約の場合は単純に名前を返す。
        // ルート集約の物理名の衝突はスキーマの検証時にエラーになるため、ここでは考えなくてよい
        if (nodeType == E_NodeType.RootAggregate) {
            return xElement.Name.LocalName;
        }

        // Child型またはChildren型、かつ名前衝突がある場合、「（直近の親のPhysicalName）の（LocalName）」
        if (nodeType == E_NodeType.ChildAggregate || nodeType == E_NodeType.ChildrenAggregate) {
            var duplicates = Document
                // まずXML要素の名前がxElementのXML要素の名前と衝突している要素を絞り込む
                .XPathSelectElements($"//{xElement.Name.LocalName}")
                // そのうちChild型またはChildren型であるものを絞り込む
                .Where(x => x != xElement
                         && (x.Attribute(ATTR_NODE_TYPE)?.Value == NODE_TYPE_CHILD
                         || x.Attribute(ATTR_NODE_TYPE)?.Value == NODE_TYPE_CHILDREN))
                .Any();
            if (duplicates) {
                // 「（直近の親のPhysicalName）の（LocalName）」
                return GetPhysicalName(xElement.Parent!) + "の" + xElement.Name.LocalName;
            }
        }

        // それ以外の場合は単純にLocalNameを返す
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
    public IEnumerable<NodeOption> GetOptions(XElement xElement) {
        var attrs = xElement.Attributes().Select(attr => attr.Name.LocalName).ToHashSet();
        return _rule.NodeOptions.Where(opt => attrs.Contains(opt.AttributeName));
    }
    /// <inheritdoc cref="SchemaParseRule.GetAvailableOptionsFor"/>
    public IEnumerable<NodeOption> GetAvailableOptionsFor(IModel model) {
        return _rule.GetAvailableOptionsFor(model);
    }
    #endregion オプション属性


    #region Aggregate
    /// <summary>
    /// XML要素と対応するモデルを返します。
    /// </summary>
    internal bool TryGetModel(XElement xElement, [NotNullWhen(true)] out IModel? model) {
        var root = xElement.AncestorsAndSelf().Reverse().Skip(1).FirstOrDefault();
        if (root == null) {
            model = null;
            return false;

        }
        var modelName = root.Attribute(ATTR_NODE_TYPE)?.Value;
        if (modelName == null) {
            model = null;
            return false;
        }
        return Models.TryGetValue(modelName, out model);
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
            return new ChildAggregate(xElement, this, previous);
        }
        if (nodeType == E_NodeType.ChildrenAggregate) {
            return new ChildrenAggregate(xElement, this, previous);
        }
        throw new InvalidOperationException($"集約ではありません: {xElement}");
    }
    /// <summary>
    /// 指定されたモデルのうち、ルート集約、Child、Childrenを列挙します。
    /// </summary>
    internal IEnumerable<XElement> EnumerateModelElements(string modelSchemaName) {
        foreach (var rootElement in Document.Root?.Elements() ?? []) {
            if (rootElement.Attribute(ATTR_NODE_TYPE)?.Value != modelSchemaName) continue;

            yield return rootElement;

            // 子孫のうちType="child"またはType="children"の要素を抽出する
            var descendantChildren = rootElement.XPathSelectElements($"descendant::*[@{ATTR_NODE_TYPE}='{NODE_TYPE_CHILD}' or @{ATTR_NODE_TYPE}='{NODE_TYPE_CHILDREN}']");
            foreach (var descendantElement in descendantChildren) {
                yield return descendantElement;
            }
        }
    }
    #endregion Aggregate


    #region ValueMember
    /// <summary>
    /// このスキーマで定義されている静的区分の種類を返します。
    /// </summary>
    /// <returns></returns>
    internal IReadOnlyDictionary<string, ValueMemberTypes.StaticEnumMember> GetStaticEnumMembers() {
        return Document.Root
            ?.Elements()
            .Where(el => el.Attribute(ATTR_NODE_TYPE)?.Value == EnumDefParser.SCHEMA_NAME)
            .ToDictionary(GetPhysicalName, el => new ValueMemberTypes.StaticEnumMember(el, this))
            ?? [];
    }

    /// <summary>
    /// このスキーマで定義されている値オブジェクト型を返します。
    /// </summary>
    /// <returns></returns>
    internal IReadOnlyDictionary<string, ValueMemberTypes.ValueObjectMember> GetValueObjectMembers() {
        return Document.Root
            ?.Elements()
            .Where(el => el.Attribute(ATTR_NODE_TYPE)?.Value == ValueObjectModel.SCHEMA_NAME)
            .ToDictionary(GetPhysicalName, el => new ValueMemberTypes.ValueObjectMember(el, this))
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
        // 値オブジェクト型
        foreach (var type in GetValueObjectMembers().Values) {
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

        // 値オブジェクト型
        if (GetValueObjectMembers().TryGetValue(type.Value, out var valueObjectMember)) {
            valueMemberType = valueObjectMember;
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
        var xPath = type.Value.Split(':')[1];
        return Document.Root?.XPathSelectElement(xPath);
    }
    /// <summary>
    /// 引数の集約を参照している集約を探して返します。
    /// </summary>
    internal IEnumerable<XElement> FindRefFrom(XElement xElement) {
        // 完全なパスを構築
        var fullPath = string.Join("/", xElement.AncestorsAndSelf().Reverse().Skip(1).Select(GetPhysicalName));

        // 完全なパスによる参照のみを検索
        return Document.XPathSelectElements($"//*[@{ATTR_NODE_TYPE}='{NODE_TYPE_REFTO}:{fullPath}']") ?? [];
    }
    #endregion RefTo


    #region 検証
    /// <summary>
    /// XMLドキュメントがスキーマ定義として不正な状態を持たないかを検証し、
    /// 検証に成功した場合はアプリケーションスキーマのインスタンスを返します。
    /// </summary>
    /// <param name="xDocument">XMLドキュメント</param>
    /// <param name="schema">作成完了後のスキーマ</param>
    /// <param name="errors">エラー</param>
    /// <returns>スキーマの作成に成功したかどうか</returns>
    public bool TryBuildSchema(XDocument xDocument, out ApplicationSchema schema, out ValidationError[] errors) {
        schema = new ApplicationSchema(xDocument, this);
        var errorsList = new List<(XElement, string ErrorMessage)>();
        var attributeErrors = new List<(XElement, string AttributeName, string ErrorMessage)>();

        // ルート集約の物理名の衝突チェック
        var rootAggregates = xDocument.Root?.Elements() ?? [];
        var rootPhysicalNames = new Dictionary<string, XElement>();

        foreach (var root in rootAggregates) {
            var rootName = GetPhysicalName(root);
            if (rootPhysicalNames.TryGetValue(rootName, out var existingRoot)) {
                errorsList.Add((root, $"ルート集約の物理名'{rootName}'が重複しています。モデルをまたいでの重複はできません。"));
            } else {
                rootPhysicalNames[rootName] = root;
            }
        }

        // 同じテーブル名を複数の集約で定義することはできない
        var tableNameGroups = xDocument.Root
            ?.Descendants()
            .Where(el => GetNodeType(el).HasFlag(E_NodeType.Aggregate))
            .GroupBy(el => GetDbName(el))
            ?? [];
        foreach (var group in tableNameGroups) {
            if (group.Count() == 1) continue;
            foreach (var el in group) {
                errorsList.Add((el, $"同じテーブル名'{group.Key}'を複数の集約で定義することはできません。"));
            }
        }

        foreach (var el in xDocument.Root?.Descendants() ?? []) {

            var nodeType = GetNodeType(el);
            var typeAttrValue = el.Attribute(ATTR_NODE_TYPE)?.Value ?? string.Empty;

            // 同じ親のメンバー同士での物理名の重複チェック
            if (el.Parent != null && el.Parent != el.Document?.Root) {
                var siblings = el.Parent.Elements().ToList();
                var siblingPhysicalNames = new Dictionary<string, XElement>();

                foreach (var sibling in siblings) {
                    var physicalName = GetPhysicalName(sibling);
                    if (siblingPhysicalNames.TryGetValue(physicalName, out var existingSibling) && existingSibling != sibling) {
                        errorsList.Add((sibling, $"同じ親の下で物理名'{physicalName}'が重複しています。"));
                    } else {
                        siblingPhysicalNames[physicalName] = sibling;
                    }
                }
            }

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
                    // 主キー属性のチェック
                    if (el.Elements().Any(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) != null)) {
                        if (TryGetModel(el, out var childModel) && childModel is DataModel) {
                            errorsList.Add((el, $"データモデルの子集約には主キー属性を付与することができません。"));
                        }
                    }
                    break;

                // Children
                case E_NodeType.ChildrenAggregate:
                    // データモデルの子配列は必ず1個以上の主キーが必要
                    if (TryGetModel(el, out var childrenModel) && childrenModel is DataModel) {
                        if (el.Elements().All(member => member.Attribute(BasicNodeOptions.IsKey.AttributeName) == null)) {
                            errorsList.Add((el, "データモデルの子配列は必ず1個以上の主キーを持たなければなりません。"));
                        }
                    }
                    break;

                // ValueMember単位の検証
                case E_NodeType.ValueMember:
                    if (TryResolveMemberType(el, out var vmType)) {
                        vmType.Validate(el, this, (el, err) => errorsList.Add((el, err)));
                    } else {
                        errorsList.Add((el, $"種類'{el.Attribute(ATTR_NODE_TYPE)?.Value}'を特定できません。"));
                    }
                    break;

                case E_NodeType.Ref:
                    // 外部参照のチェック
                    if (!ValidateRefTo(el, out var refError)) {
                        errorsList.Add((el, refError));
                    }
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
                    AddError = err => attributeErrors.Add((el, opt.AttributeName, err)),
                    SchemaParseContext = this,
                });
            }
        }

        // エラーをXML要素ごとにまとめる
        errors = errorsList
            .GroupBy(x => x.Item1)
            .Select(x => new ValidationError {
                XElement = x.Key,
                OwnErrors = x.Select(y => y.ErrorMessage).ToArray(),
                AttributeErrors = attributeErrors
                    .Where(y => y.Item1 == x.Key)
                    .GroupBy(y => y.AttributeName)
                    .ToDictionary(y => y.Key, y => y.Select(z => z.ErrorMessage).ToArray()),
            })
            .ToArray();
        return errors.Length == 0;
    }

    /// <summary>
    /// 外部参照のバリデーションを行います
    /// </summary>
    /// <param name="refElement">チェック対象のref-to要素</param>
    /// <param name="errorMessage">エラーメッセージ（エラーがある場合）</param>
    /// <returns>バリデーションが成功したかどうか</returns>
    private bool ValidateRefTo(XElement refElement, out string errorMessage) {
        errorMessage = string.Empty;

        // ref-to:の後ろの部分を取得
        var typeAttr = refElement.Attribute(ATTR_NODE_TYPE);
        if (typeAttr == null || !typeAttr.Value.StartsWith(NODE_TYPE_REFTO + ":")) {
            errorMessage = $"ref-to要素のType属性が正しくありません: {typeAttr?.Value}";
            return false;
        }

        // 参照先の要素を見つける
        var refTo = FindRefTo(refElement);
        if (refTo == null) {
            errorMessage = $"参照先が見つかりません: {typeAttr.Value}";
            return false;
        }

        // 自身のツリーの集約を参照していないかチェック
        var rootElement = refElement.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);
        var refToRoot = refTo.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);

        if (rootElement == refToRoot) {
            errorMessage = "自身のツリーの集約を参照することはできません。";
            return false;
        }

        // モデルの種類に基づく参照制約チェック
        if (TryGetModel(refElement, out var model)) {
            if (model is DataModel) {
                // データモデルからはデータモデルの集約しか参照できない
                if (TryGetModel(refTo, out var refToModel) && !(refToModel is DataModel)) {
                    errorMessage = "データモデルの集約からはデータモデルの集約しか参照できません。";
                    return false;
                }
            } else if (model is QueryModel) {
                // クエリモデルからはクエリモデルの集約しか参照できない
                if (TryGetModel(refTo, out var refToModel) && !(refToModel is QueryModel)) {
                    // GenerateDefaultQueryModelの値をより厳密に検証
                    var isGDQM = HasGenerateDefaultQueryModelAttribute(refTo);

                    if (!isGDQM) {
                        errorMessage = $"クエリモデルの集約からはクエリモデルの集約または{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}属性が付与されたデータモデルしか参照できません。";
                        return false;
                    }
                }

                // クエリモデルで循環参照を定義することはできない
                if (HasCircularReference(refElement, refTo)) {
                    errorMessage = "クエリモデルで循環参照を定義することはできません。";
                    return false;
                }
            } else if (model is CommandModel) {
                // コマンドモデルからはクエリモデルの集約しか参照できない
                if (TryGetModel(refTo, out var refToModel) && !(refToModel is QueryModel)) {
                    // GenerateDefaultQueryModelの値をより厳密に検証
                    var isGDQM = HasGenerateDefaultQueryModelAttribute(refTo);

                    if (!isGDQM) {
                        errorMessage = $"コマンドモデルの集約からはクエリモデルの集約または{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}属性が付与されたデータモデルしか参照できません。";
                        return false;
                    }
                }

                // RefToObjectの指定がないとエラー
                if (refElement.Attribute(BasicNodeOptions.RefToObject.AttributeName) == null) {
                    errorMessage = $"コマンドモデルからクエリモデルを外部参照する場合、{BasicNodeOptions.RefToObject.AttributeName}属性を指定する必要があります。";
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 循環参照をチェックします
    /// </summary>
    /// <param name="source">参照元要素</param>
    /// <param name="target">参照先要素</param>
    /// <returns>循環参照がある場合true</returns>
    private bool HasCircularReference(XElement source, XElement target) {
        var visited = new HashSet<XElement>();
        var queue = new Queue<XElement>();

        queue.Enqueue(target);

        while (queue.Count > 0) {
            var current = queue.Dequeue();

            if (!visited.Add(current)) {
                continue;
            }

            // 参照を探す
            foreach (var el in current.Descendants()) {
                var typeAttr = el.Attribute(ATTR_NODE_TYPE);
                if (typeAttr != null && typeAttr.Value.StartsWith(NODE_TYPE_REFTO + ":")) {
                    var refTo = FindRefTo(el);
                    if (refTo != null) {
                        if (refTo == source) {
                            // 循環参照発見
                            return true;
                        }
                        queue.Enqueue(refTo);
                    }
                }
            }
        }

        return false;
    }

    public class ValidationError {
        public required XElement XElement { get; init; }
        /// <summary>
        /// XML要素自体に対するエラー
        /// </summary>
        public required string[] OwnErrors { get; init; }
        /// <summary>
        /// 属性に対するエラー
        /// </summary>
        public required IReadOnlyDictionary<string, string[]> AttributeErrors { get; init; }

        public override string ToString() {
            return $"{XElement.AncestorsAndSelf().Reverse().Select(el => el.Name.LocalName).Join("/")}: {OwnErrors.Join(", ")}";
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

    /// <summary>
    /// 要素のルート集約要素を返します
    /// </summary>
    internal XElement GetRootAggregateElement(XElement element) {
        return element.AncestorsAndSelf().Last(e => e.Parent == e.Document?.Root);
    }

    /// <summary>
    /// 要素またはその親のルート集約にGenerateDefaultQueryModel属性が付与されているかを確認します
    /// </summary>
    internal bool HasGenerateDefaultQueryModelAttribute(XElement element) {
        var rootElement = GetRootAggregateElement(element);
        var gdqmAttr = rootElement.Attribute(BasicNodeOptions.GenerateDefaultQueryModel.AttributeName)?.Value;
        return !string.IsNullOrEmpty(gdqmAttr) && gdqmAttr.Equals("True", StringComparison.OrdinalIgnoreCase);
    }
}
