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
using System.Collections.Concurrent;

namespace Nijo.SchemaParsing;

/// <summary>
/// XML要素をこのアプリケーションのルールに従って解釈する
/// </summary>
public class SchemaParseContext {
    public SchemaParseContext(XDocument xDocument, SchemaParseRule rule, GeneratedProjectOptions projectOptions) {
        // ルールの検証
        rule.ThrowIfInvalid();

        Document = xDocument;
        ProjectOptions = projectOptions;

        // 拡張メソッドの方の GetDisplayName からこのコンテキストの
        // GetDisplayName を呼び出すために、XDocument にこのコンテキストをアノテーションとして付与する。
        Document.AddAnnotation(this);

        Models = rule.Models.ToDictionary(m => m.SchemaName);
        _rule = rule;
        _valueMemberTypes = rule.ValueMemberTypes.ToDictionary(m => m.SchemaTypeName);
        _elementCountByLocalName = new Lazy<IReadOnlyDictionary<string, int>>(() => Document
            .Descendants()
            .GroupBy(el => el.Name.LocalName)
            .ToDictionary(group => group.Key, group => group.Count()));
        _genericLookupTableCategoriesCache = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<GenericLookupTableParser.GenericLookupTableCategory>>>(
            ParseGenericLookupTableCategories,
            isThreadSafe: true);
    }

    public XDocument Document { get; }
    public GeneratedProjectOptions ProjectOptions { get; }
    public IReadOnlyDictionary<string, IModel> Models { get; }
    private readonly SchemaParseRule _rule;
    private readonly IReadOnlyDictionary<string, IValueMemberType> _valueMemberTypes;
    /// <summary>enum, value-object を除いた値型の一覧</summary>
    public IEnumerable<IValueMemberType> ValueMemberTypes => _rule.ValueMemberTypes;

    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、データ構造をもつモデル（Data Model, Query Model, Structure Model）が格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_DATA_STRUCTURES = "DataStructures";
    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、 Command Model が格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_COMMANDS = "Commands";
    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、静的区分が格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_STATIC_ENUMS = "StaticEnums";
    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、値オブジェクトが格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_VALUE_OBJECTS = "ValueObjects";
    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、定数が格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_CONSTANTS = "Constants";
    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、カスタム属性が格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_CUSTOM_ATTRIBUTES = "CustomAttributes";
    /// <summary>
    /// nijo.xml のルート要素直下の要素のうち、汎用参照テーブルのカテゴリが格納されるXML要素の名前
    /// </summary>
    internal const string SECTION_GENERIC_LOOKUP_TABLES = "GenericLookupTableCategories";

    /// <summary>
    /// nijo.xml のルート要素直下のセクション名を、XMLに記載される順序で列挙する。
    /// </summary>
    internal static IEnumerable<string> GetAllSectionNames() {
        yield return SECTION_DATA_STRUCTURES;
        yield return SECTION_COMMANDS;
        yield return SECTION_STATIC_ENUMS;
        yield return SECTION_VALUE_OBJECTS;
        yield return SECTION_CONSTANTS;
        yield return SECTION_CUSTOM_ATTRIBUTES;
    }

    /// <summary>
    /// 要素を一意に識別するための属性名。
    /// GUI上で新たに作成されて以降決して変更されない前提。
    /// </summary>
    internal const string ATTR_UNIQUE_ID = "UniqueId";
    internal const string ATTR_NODE_TYPE = "Type";

    internal const string NODE_TYPE_CHILD = "child";
    internal const string NODE_TYPE_CHILDREN = "children";
    internal const string NODE_TYPE_REFTO = "ref-to";

    internal sealed class OriginalTypeAnnotation {
        internal OriginalTypeAnnotation(string typeName) {
            TypeName = typeName;
        }
        internal string TypeName { get; }
    }

    /// <summary>
    /// 物理名。スキーマ内での物理名の衝突を考慮した値を返す。
    /// </summary>
    internal string GetDisplayName(XElement xElement) {
        return _displayNameCache.GetOrAdd(xElement, el => {
            if (el.Attribute(BasicNodeOptions.DisplayNameIsEmpty.AttributeName) != null) {
                return string.Empty;
            }

            return el.Attribute(BasicNodeOptions.DisplayName.AttributeName)?.Value ?? el.Name.LocalName;
        });
    }
    private readonly ConcurrentDictionary<XElement, string> _displayNameCache = new();

    internal IReadOnlyList<GenericLookupTableParser.GenericLookupTableCategory> GetGenericLookupTableCategories(string uniqueId) {
        return _genericLookupTableCategoriesCache.Value.GetValueOrDefault(uniqueId)
            ?? Array.Empty<GenericLookupTableParser.GenericLookupTableCategory>();
    }

    private IReadOnlyDictionary<string, IReadOnlyList<GenericLookupTableParser.GenericLookupTableCategory>> ParseGenericLookupTableCategories() {
        var section = Document.Root?.Element(SECTION_GENERIC_LOOKUP_TABLES);
        if (section == null) return new Dictionary<string, IReadOnlyList<GenericLookupTableParser.GenericLookupTableCategory>>(StringComparer.Ordinal);

        var categoriesByUniqueId = new Dictionary<string, IReadOnlyList<GenericLookupTableParser.GenericLookupTableCategory>>(StringComparer.Ordinal);

        foreach (var categoriesElement in section.Elements(GenericLookupTableParser.CATEGORIES)) {
            var uniqueId = categoriesElement.Attribute(GenericLookupTableParser.FOR)?.Value;
            if (string.IsNullOrEmpty(uniqueId)) continue;

            var categories = new List<GenericLookupTableParser.GenericLookupTableCategory>();
            foreach (var categoryElement in categoriesElement.Elements()) {
                var keys = new List<GenericLookupTableParser.GenericLookupTableCategory.HardCodedKeyEntry>();
                foreach (var keyElement in categoryElement.Elements(GenericLookupTableParser.KEY)) {
                    var keyFor = keyElement.Attribute(GenericLookupTableParser.FOR)?.Value;
                    var keyValue = keyElement.Attribute(GenericLookupTableParser.KEY_VALUE)?.Value;
                    if (!string.IsNullOrEmpty(keyFor) && keyValue != null) {
                        keys.Add(new GenericLookupTableParser.GenericLookupTableCategory.HardCodedKeyEntry {
                            UniqueId = keyFor,
                            Value = keyValue,
                        });
                    }
                }

                categories.Add(new GenericLookupTableParser.GenericLookupTableCategory {
                    Name = categoryElement.Name.LocalName,
                    DisplayName = GetDisplayName(categoryElement),
                    HardCodedKeys = keys,
                });
            }

            categoriesByUniqueId.TryAdd(uniqueId, categories);
        }

        return categoriesByUniqueId;
    }
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<GenericLookupTableParser.GenericLookupTableCategory>>> _genericLookupTableCategoriesCache;

    /// <summary>
    /// 物理名。スキーマ内での物理名の衝突を考慮した値を返す。
    /// </summary>
    internal string GetPhysicalName(XElement xElement) {
        return _physicalNameCache.GetOrAdd(xElement, el => {
            var nodeType = GetNodeType(el);

            // ルート集約の場合は単純に名前を返す。
            // ルート集約の物理名の衝突はスキーマの検証時にエラーになるため、ここでは考えなくてよい
            if (nodeType == E_NodeType.RootAggregate) {
                return el.Name.LocalName;
            }

            // Child型またはChildren型、かつ名前衝突がある場合、「（直近の親のPhysicalName）の（LocalName）」
            if (nodeType == E_NodeType.ChildAggregate || nodeType == E_NodeType.ChildrenAggregate) {
                var duplicates = _elementCountByLocalName.Value.TryGetValue(el.Name.LocalName, out var count)
                    && count > 1;
                if (duplicates) {
                    // 「（直近の親のPhysicalName）の（LocalName）」
                    return GetPhysicalName(el.Parent!) + "の" + el.Name.LocalName;
                }
            }

            // それ以外の場合は単純にLocalNameを返す
            return el.Name.LocalName;
        });
    }
    private readonly ConcurrentDictionary<XElement, string> _physicalNameCache = new();
    private readonly Lazy<IReadOnlyDictionary<string, int>> _elementCountByLocalName;


    /// <summary>
    /// XML要素の種類を判定する。
    /// 編集途中の不正な状態であっても入力内容検査等に使う必要があるため、
    /// XML要素が不正であっても例外を出さない。
    /// </summary>
    internal E_NodeType GetNodeType(XElement xElement) {
        return _nodeTypeCache.GetOrAdd(xElement, el => {
            // ルート集約, Child, Children
            if (el.TryGetAggregateNodeType(out var aggregateNodeType)) {
                return aggregateNodeType.Value;
            }

            // 親がenumセクションの直下にあるなら静的区分の値
            var xElementParent = el.Parent;
            if (xElementParent != null
                && xElementParent.Parent?.Parent == Document.Root
                && xElementParent.Parent?.Name.LocalName == SECTION_STATIC_ENUMS) {
                return E_NodeType.StaticEnumValue;
            }

            // 以降はType属性の値で区別
            var type = el.Attribute(ATTR_NODE_TYPE);
            if (type == null) {
                return E_NodeType.Unknown;
            }

            // RefTo
            if (type.Value.StartsWith(NODE_TYPE_REFTO)) {
                return E_NodeType.Ref;
            }
            // ValueMember
            if (TryResolveMemberType(el, out _)) {
                return E_NodeType.ValueMember;
            }
            return E_NodeType.Unknown;
        });
    }
    private readonly ConcurrentDictionary<XElement, E_NodeType> _nodeTypeCache = new();


    #region オプション属性
    private const string IS_DYNAMIC_ENUM_MODEL = "dynamic-enum";
    /// <summary>
    /// XML要素に定義されているオプション属性を返します。
    /// </summary>
    public IEnumerable<NodeOption> GetOptions(XElement xElement) {
        var attrs = xElement.Attributes().Select(attr => attr.Name.LocalName).ToHashSet();
        return _rule.NodeOptions.Where(opt => attrs.Contains(opt.AttributeName));
    }
    /// <summary>
    /// このルールで定義されているすべてのNodeOptionを返します。
    /// </summary>
    public IEnumerable<NodeOption> GetAllNodeOptions() {
        foreach (var opt in _rule.NodeOptions) {
            yield return opt;
        }
    }

    /// <summary>
    /// XML要素に定義されているカスタム属性を返します。
    /// </summary>
    public IEnumerable<NijoXmlCustomAttribute> GetCustomAttributes(XElement xElement) {
        var attrDefs = _customAttributesCache ??= NijoXmlCustomAttribute
            .FromXDocument(Document)
            .ToArray();
        var specifiedAttrCustomIds = xElement
            .Attributes()
            .Select(attr => attr.Name.LocalName)
            .ToHashSet();
        return attrDefs
            .Where(def => specifiedAttrCustomIds.Contains(def.UniqueId!));
    }
    private NijoXmlCustomAttribute[]? _customAttributesCache;
    #endregion オプション属性


    #region Aggregate
    /// <summary>
    /// XML要素と対応するモデルを返します。
    /// </summary>
    internal bool TryGetModel(XElement xElement, [NotNullWhen(true)] out IModel? model) {
        var root = xElement.GetRootAggregateElement();
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
    #endregion Aggregate


    #region ValueMember
    /// <summary>
    /// このスキーマで定義されている静的区分の種類を返します。
    /// </summary>
    /// <returns></returns>
    internal IReadOnlyDictionary<string, ValueMemberTypes.StaticEnumMember> GetStaticEnumMembers() {
        return Document.Root
            ?.Element(SECTION_STATIC_ENUMS)
            ?.Elements()
            .ToDictionary(GetPhysicalName, el => new ValueMemberTypes.StaticEnumMember(el, this))
            ?? [];
    }

    /// <summary>
    /// このスキーマで定義されている値オブジェクト型を返します。
    /// </summary>
    /// <returns></returns>
    internal IReadOnlyDictionary<string, ValueMemberTypes.ValueObjectMember> GetValueObjectMembers() {
        return Document.Root
            ?.Element(SECTION_VALUE_OBJECTS)
            ?.Elements()
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
        var xPath = $"//{SECTION_DATA_STRUCTURES}/{type.Value.Split(':')[1]}";
        return _refToCache.GetOrAdd(xPath, path => {
            return Document.Root?.XPathSelectElement(path);
        });
    }
    /// <summary>
    /// 引数の集約を参照している集約を探して返します。
    /// </summary>
    internal IEnumerable<XElement> FindRefFrom(XElement xElement) {
        return _refFromCache.GetOrAdd(xElement, el => {
            var fullPath = string.Join("/", el.AncestorsAndSelf().Reverse().Skip(2).Select(GetPhysicalName));
            return Document.XPathSelectElements($"//{SECTION_DATA_STRUCTURES}//*[@{ATTR_NODE_TYPE}='{NODE_TYPE_REFTO}:{fullPath}']")?.ToArray() ?? [];
        });
    }
    private readonly ConcurrentDictionary<string, XElement?> _refToCache = new();
    private readonly ConcurrentDictionary<XElement, IReadOnlyCollection<XElement>> _refFromCache = new();
    #endregion RefTo


    #region 検証
    /// <summary>
    /// <see cref="TryBuildSchema"/> の仕様をレンダリングする。ドキュメント用。
    /// </summary>
    internal static string RenderValidationSpecificationMarkdown() {
        return $$"""
            - 複数のルート集約の間で物理名が重複していてはいけません。
            - 同じ親に属するメンバーで同じ物理名が重複していてはいけません。
            """;
    }
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

        // カスタム属性の定義の検証
        var customAttributeElements = xDocument.Root?.Element(SECTION_CUSTOM_ATTRIBUTES)?.Elements().ToArray() ?? [];
        var customAttributes = NijoXmlCustomAttribute.FromXDocument(xDocument).ToArray();
        var nodeOptionsByName = _rule.NodeOptions.ToDictionary(opt => opt.AttributeName);
        var customAttributesByUniqueId = customAttributes
            .Where(attr => !string.IsNullOrWhiteSpace(attr.UniqueId))
            .GroupBy(attr => attr.UniqueId!)
            .ToDictionary(group => group.Key, group => group.First());
        if (customAttributeElements.Length == customAttributes.Length) {
            for (int i = 0; i < customAttributes.Length; i++) {
                var attr = customAttributes[i];
                var el = customAttributeElements[i];
                foreach (var error in attr.ValidateThis(this, _rule)) {
                    errorsList.Add((el, error));
                }
            }

            foreach (var attr in customAttributes.Where(a => a.IsValidation)) {
                if (string.IsNullOrWhiteSpace(attr.PhysicalName)) continue;
                if (!attr.PhysicalName.IsCSharpSafe()) {
                    var el = customAttributeElements.FirstOrDefault(e => e.Name.LocalName == attr.UniqueId) ?? xDocument.Root ?? new XElement("root");
                    errorsList.Add((el, $"カスタム属性 '{attr.PhysicalName}' の {nameof(attr.PhysicalName)} はC#の識別子として不正です。英数字とアンダースコアのみを使用し、先頭は英字またはアンダースコアにしてください。"));
                }
            }
        }

        // ルート集約の物理名の衝突チェック
        var rootAggregates = xDocument.Root
            ?.Elements()
            .Where(el => el.Name.LocalName != SECTION_CUSTOM_ATTRIBUTES)
            .SelectMany(el => el.Elements())
            ?? [];
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
            ?.Element(SECTION_DATA_STRUCTURES)
            ?.DescendantsAndSelf()
            .Where(el => GetNodeType(el).HasFlag(E_NodeType.Aggregate)
                      && TryGetModel(el, out var model) && model is DataModel)
            .GroupBy(el => el.GetDbName())
            ?? [];
        foreach (var group in tableNameGroups) {
            if (group.Count() == 1) continue;
            foreach (var el in group) {
                errorsList.Add((el, $"同じテーブル名'{group.Key}'を複数の集約で定義することはできません。"));
            }
        }

        // 同じ親のメンバー同士での物理名の重複チェック
        var targetElements = xDocument.Root
            ?.Element(SECTION_DATA_STRUCTURES)
            ?.Elements()
            .SelectMany(el => el.DescendantsAndSelf())
            ?? [];
        foreach (var el in targetElements) {

            var nodeType = GetNodeType(el);
            var typeAttrValue = el.Attribute(ATTR_NODE_TYPE)?.Value ?? string.Empty;

            var elParent = el.Parent;
            if (elParent != null && elParent.Parent != el.Document?.Root) {
                var siblings = elParent.Elements().ToList();
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
                    // ConstantModelの場合はType属性未指定でもエラーにしない（ConstantTypeで判定しているため）
                    if (TryGetModel(el, out var constantModel) && constantModel is ConstantModel) {
                        // ConstantModelの定数要素はType属性未指定でも許可
                        break;
                    }

                    if (string.IsNullOrEmpty(typeAttrValue)) {
                        attributeErrors.Add((el, ATTR_NODE_TYPE, $"ノードの種類が不明です。{ATTR_NODE_TYPE}属性が指定されているか確認してください。"));
                    } else {
                        attributeErrors.Add((el, ATTR_NODE_TYPE, $"ノードの種類 '{typeAttrValue}' は有効ではありません。"));
                    }
                    break;

                // ルート集約の場合
                case E_NodeType.RootAggregate:
                    var model = Models.GetValueOrDefault(typeAttrValue);
                    if (model == null) {
                        attributeErrors.Add((el, ATTR_NODE_TYPE, $"{ATTR_NODE_TYPE}属性でモデルが指定されていません。使用できる値は {Models.Keys.Join(", ")} です。"));
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
                        attributeErrors.Add((el, ATTR_NODE_TYPE, $"種類'{el.Attribute(ATTR_NODE_TYPE)?.Value}'を特定できません。"));
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

            // 属性に基づくチェック
            foreach (var attr in el.Attributes()) {
                // 名前空間宣言はスキーマのルールで定義された属性ではないのでスキップ
                if (attr.IsNamespaceDeclaration) continue;

                var attrName = attr.Name.LocalName;
                var hasModel = TryGetModel(el, out var optModel);

                // 特殊な属性
                if (attrName == ATTR_NODE_TYPE || attrName == ATTR_UNIQUE_ID) {
                    // チェック対象外
                }

                // 既定の属性
                else if (nodeOptionsByName.TryGetValue(attrName, out var opt)) {

                    // モデルとノード種別の組み合わせで利用可能かチェック
                    if (hasModel && !opt.IsAvailable(optModel!, nodeType)) {
                        attributeErrors.Add((el, attrName, $"この属性はこの要素には指定できません。"));
                        continue; // 指定不可な属性なので、それ以上の検証はスキップ
                    }

                    // 複雑な検証ロジック
                    opt.ValidateOthers(new() {
                        Value = attr.Value,
                        XElement = el,
                        NodeType = nodeType,
                        AddError = err => attributeErrors.Add((el, attrName, err)),
                        SchemaParseContext = this,
                    });
                }

                // カスタム属性
                else if (customAttributesByUniqueId.TryGetValue(attrName, out var customAttr)) {
                    if (hasModel) {
                        if (!customAttr.AvailableModels.Contains(optModel!.SchemaName)) {
                            attributeErrors.Add((el, attrName, $"カスタム属性 '{customAttr.PhysicalName}' はモデル '{optModel!.SchemaName}' では使用できません。"));
                        } else {
                            foreach (var error in customAttr.ValidateModelElement(el)) {
                                attributeErrors.Add((el, attrName, error));
                            }
                        }
                    }
                }

                // 定義されていない属性
                else {
                    attributeErrors.Add((el, attrName, "この属性は定義されていません。"));
                }
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
            .Concat(attributeErrors // attributeErrors にしかエラーがない要素も拾い上げる
                .Select(x => x.Item1)
                .Distinct()
                .Where(x => !errorsList.Any(y => y.Item1 == x)) // errorsList に含まれない要素のみを対象とする
                .Select(x => new ValidationError {
                    XElement = x,
                    OwnErrors = Array.Empty<string>(), // errorsList にはないので空配列
                    AttributeErrors = attributeErrors
                        .Where(y => y.Item1 == x)
                        .GroupBy(y => y.AttributeName)
                        .ToDictionary(y => y.Key, y => y.Select(z => z.ErrorMessage).ToArray()),
                }))
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
        var rootElement = refElement.GetRootAggregateElement();
        var refToRoot = refTo.GetRootAggregateElement();

        if (rootElement == refToRoot) {
            errorMessage = "自身のツリーの集約を参照することはできません。";
            return false;
        }

        // モデルの種類に基づく参照制約チェック
        if (TryGetModel(refElement, out var model)) {
            if (model is DataModel) {
                // データモデルからはデータモデルの集約、
                // またはビューにマッピングされるクエリモデルしか参照できない
                if (TryGetModel(refTo, out var refToModel)
                    && refToModel is not DataModel
                    && (refToModel is not QueryModel || refToRoot.Attribute(BasicNodeOptions.MapToView.AttributeName) == null)) {
                    errorMessage = "データモデルの集約からはデータモデルの集約またはビューにマッピングされるクエリモデルしか参照できません。";
                    return false;
                }

                // GDQM -> 非GDQM の参照を禁止。RefTargetなどが生成されないので
                if (rootElement.HasGenerateDefaultQueryModelAttribute()
                    && TryGetModel(refTo, out var refToDataModel)
                    && refToDataModel is DataModel
                    && !refToRoot.HasGenerateDefaultQueryModelAttribute()) {
                    errorMessage = $"{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}属性が付与されたデータモデルの集約からは、同じく{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}属性が付与されたデータモデルの集約しか参照できません。";
                    return false;
                }
            } else if (model is QueryModel) {
                // クエリモデルからはクエリモデルの集約しか参照できない
                if (TryGetModel(refTo, out var refToModel) && !(refToModel is QueryModel)) {
                    // GenerateDefaultQueryModelの値をより厳密に検証
                    var isGDQM = refTo.HasGenerateDefaultQueryModelAttribute();

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
                    var isGDQM = refTo.HasGenerateDefaultQueryModelAttribute();

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
            } else if (model is StructureModel) {
                // StructureModelからはクエリモデル、GDQMデータモデル、またはStructureModelの集約しか参照できない
                if (TryGetModel(refTo, out var refToModel)) {
                    var isQueryModel = refToModel is QueryModel;
                    var isGDQM = refTo.HasGenerateDefaultQueryModelAttribute();
                    var isStructureModel = refToModel is StructureModel;

                    if (!isQueryModel && !isGDQM && !isStructureModel) {
                        errorMessage = $"StructureModelの集約からはクエリモデル、{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}属性が付与されたデータモデル、またはStructureModelの集約しか参照できません。";
                        return false;
                    }

                    // クエリモデルまたはGDQMデータモデルを参照する場合はRefToObjectの指定が必須
                    if ((isQueryModel || isGDQM) && refElement.Attribute(BasicNodeOptions.RefToObject.AttributeName) == null) {
                        errorMessage = $"StructureModelからクエリモデルを外部参照する場合、{BasicNodeOptions.RefToObject.AttributeName}属性を指定する必要があります。";
                        return false;
                    }
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
}
