using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Nijo.CodeGenerating;
using Nijo.SchemaParsing;
using Nijo.WebService.Previewing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// スキーマ定義編集画面が扱う、nijo.xml とその同階層に置かれる固定名ファイル群
/// （nijo.viewState.json, nijo.preview.json）の内容の組。
/// nijo.xml の内容に加え、編集画面が必要とする固定ルール（属性定義など）も含む。
/// ※プロジェクトのディレクトリ・ファイルパスを表す <see cref="Nijo.GeneratedProject"/> とは別物。
/// </summary>
public class GeneratedProjectInGui {

    // --- nijo.xml 由来の編集対象 ---

    [JsonPropertyName("xmlElementTrees")]
    public List<RootAggregateXmlTree> XmlElementTrees { get; set; } = [];
    [JsonPropertyName("customAttributes")]
    public List<NijoXmlCustomAttribute> CustomAttributes { get; set; } = [];
    [JsonPropertyName("projectOptions")]
    public JsonObject ProjectOptions { get; set; } = new();
    [JsonPropertyName("genericLookupTableCategories")]
    public List<GenericLookupTableCategories> GenericLookupTableCategories { get; set; } = [];

    // --- 固定ルール（SchemaParseRule.Default 由来。画面上は読み取り専用） ---

    /// <summary>
    /// nijo.xml のXML要素に定義できる属性の一覧。
    /// ほぼ <see cref="Nijo.SchemaParsing.NodeOption"/> とだいたい同じ。
    /// この一覧にはカスタム属性は含まれない。
    /// </summary>
    [JsonPropertyName("attributeDefs")]
    public List<XmlAttributeDef> AttributeDefs { get; set; } = [];
    [JsonPropertyName("valueMemberTypes")]
    public List<ValueMemberType> ValueMemberTypes { get; set; } = [];
    [JsonPropertyName("projectOptionPropertyInfos")]
    public List<ProjectOptionPropertyInfo> ProjectOptionPropertyInfos { get; set; } = [];

    // --- nijo.xml と同階層の別ファイルの内容 ---

    /// <summary>
    /// スキーマ定義グラフの見た目の状態（nijo.viewState.jsonの内容）。
    /// nullの場合は保存をスキップする。
    /// </summary>
    [JsonPropertyName("schemaGraphViewState")]
    public SchemaGraphViewState? SchemaGraphViewState { get; set; }

    /// <summary>
    /// 生成後アプリのデバッグ起動設定（nijo.preview.jsonの内容）
    /// </summary>
    [JsonPropertyName("previewSetting")]
    public PreviewSetting PreviewSetting { get; set; } = new();

    /// <summary>
    /// 新しい <see cref="XDocument"/> インスタンスを構築して返す。
    /// AttributeDefs・ValueMemberTypes・ProjectOptionPropertyInfosは、暫定的に、画面上で編集できないものとし、
    /// <see cref="Nijo.SchemaParsing.SchemaParseRule.Default"/> から取得する。
    /// </summary>
    internal bool TryConvertToXDocument(
        XDocument original,
        ICollection<string> errors,
        [NotNullWhen(true)] out XDocument? xDocument,
        [NotNullWhen(true)] out IReadOnlyDictionary<XElement, string>? uuidToXmlElement) {

        xDocument = new XDocument(original);
        if (xDocument.Root == null) {
            errors.Add("XMLにルート要素がありません");
            xDocument = null;
            uuidToXmlElement = null;
            return false;
        }

        // クローンしたXDocumentのルート要素の子を削除する。
        // この後の処理で、ルート要素の子を追加していく。
        xDocument.Root.RemoveNodes();

        // セクションを作成
        var sections = new Dictionary<string, XElement>();
        foreach (var sectionName in SchemaParsing.SchemaParseContext.GetAllSectionNames()) {
            var section = new XElement(sectionName);
            sections[sectionName] = section;
            xDocument.Root.Add(section);
        }

        // ルート要素の子を追加していく過程で、XElementと、そのXElementの元となったJSONのIdを紐づける。
        var mapping = new Dictionary<XElement, string>();

        for (int i = 0; i < XmlElementTrees.Count; i++) {
            var aggregateTree = XmlElementTrees[i];
            var logName = aggregateTree.XmlElements.Count > 0 && !string.IsNullOrWhiteSpace(aggregateTree.XmlElements[0].LocalName)
                ? $"{aggregateTree.XmlElements[0].LocalName}のツリー"
                : $"第{i + 1}番目の集約ツリー";
            if (aggregateTree.TryConvertToXElement(
                error => errors.Add($"{logName}: {error}"),
                (xElement, item) => mapping[xElement] = item.UniqueId,
                out var rootAggregate,
                out var commentToRootAggregate)) {

                // セクションへの振り分け
                var type = rootAggregate.Attribute(SchemaParsing.SchemaParseContext.ATTR_NODE_TYPE)?.Value;
                string sectionName;
                if (type == new Models.DataModel().SchemaName) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_DATA_STRUCTURES;
                } else if (type == new Models.QueryModel().SchemaName) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_DATA_STRUCTURES;
                } else if (type == new Models.CommandModel().SchemaName) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_COMMANDS;
                } else if (type == new Models.StructureModel().SchemaName) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_DATA_STRUCTURES;
                } else if (type == Models.ValueObjectModel.SCHEMA_NAME) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_VALUE_OBJECTS;
                } else if (type == Models.ConstantModel.SCHEMA_NAME) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_CONSTANTS;
                } else if (type == SchemaParsing.EnumDefParser.SCHEMA_NAME) {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_STATIC_ENUMS;
                } else {
                    sectionName = SchemaParsing.SchemaParseContext.SECTION_STATIC_ENUMS;
                }

                if (sections.TryGetValue(sectionName, out var section)) {
                    if (commentToRootAggregate != null) section.Add(commentToRootAggregate);
                    section.Add(rootAggregate);
                } else {
                    if (commentToRootAggregate != null) xDocument.Root.Add(commentToRootAggregate);
                    xDocument.Root.Add(rootAggregate);
                }
            }
        }

        // カスタム属性
        var customAttributesSection = sections[SchemaParsing.SchemaParseContext.SECTION_CUSTOM_ATTRIBUTES];
        foreach (var customAttribute in CustomAttributes) {
            foreach (var xNode in customAttribute.ToXNodes()) {
                customAttributesSection.Add(xNode);

                if (xNode is XElement xElement) {
                    mapping[xElement] = customAttribute.UniqueId ?? throw new System.InvalidOperationException("ありえない");
                }
            }
        }

        // 空のセクションを削除
        foreach (var section in sections.Values.ToList()) {
            if (!section.HasElements) {
                section.Remove();
            }
        }

        // 汎用参照テーブルのカテゴリセクションを追加
        var genericLookupSection = SchemaEditor.GenericLookupTableCategories.ToXElement(this.GenericLookupTableCategories);
        if (genericLookupSection != null) {
            xDocument.Root.Add(genericLookupSection);
        }

        if (errors.Count > 0) {
            uuidToXmlElement = null;
            return false;
        }

        uuidToXmlElement = mapping;
        return true;
    }
}
