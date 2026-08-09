using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor2;

/// <summary>
/// 汎用参照テーブル1個分のカテゴリ定義データ。
/// IsGenericLookupTable が指定されたルート集約が所有する。
/// </summary>
public class EditingGenericLookupTable {
    [JsonPropertyName("categories")]
    public List<EditingGenericLookupTableCategory> Categories { get; set; } = [];

    /// <summary>
    /// nijo.xml の GenericLookupTableCategories セクションを読み込み、対象ルート集約の UniqueId をキーにして返す。
    /// </summary>
    internal static IReadOnlyDictionary<string, EditingGenericLookupTable> FromXDocument(XDocument xDocument) {
        var result = new Dictionary<string, EditingGenericLookupTable>();
        var section = xDocument.Root?.Element(SchemaParseContext.SECTION_GENERIC_LOOKUP_TABLES);
        if (section == null) return result;

        foreach (var categoriesElement in section.Elements(GenericLookupTableParser.CATEGORIES)) {
            var forUniqueId = categoriesElement.Attribute(GenericLookupTableParser.FOR)?.Value;
            if (string.IsNullOrEmpty(forUniqueId)) continue;

            var table = new EditingGenericLookupTable();
            foreach (var categoryElement in categoriesElement.Elements()) {
                var category = new EditingGenericLookupTableCategory {
                    Name = categoryElement.Name.LocalName,
                    DisplayName = categoryElement.Attribute(BasicNodeOptions.DisplayName.AttributeName)?.Value
                        ?? categoryElement.Name.LocalName,
                };
                foreach (var keyElement in categoryElement.Elements(GenericLookupTableParser.KEY)) {
                    var keyFor = keyElement.Attribute(GenericLookupTableParser.FOR)?.Value;
                    var keyValue = keyElement.Attribute(GenericLookupTableParser.KEY_VALUE)?.Value;
                    if (!string.IsNullOrEmpty(keyFor) && keyValue != null) {
                        category.HardCodedKeyValues[keyFor] = keyValue;
                    }
                }
                table.Categories.Add(category);
            }
            result[forUniqueId] = table;
        }
        return result;
    }

    /// <summary>
    /// ルート集約ごとの汎用参照テーブル定義を GenericLookupTableCategories セクションに変換する。中身が空ならnullを返す。
    /// </summary>
    internal static XElement? ToXElement(IEnumerable<KeyValuePair<string, EditingGenericLookupTable>> tablesByRootUniqueId) {
        var section = new XElement(SchemaParseContext.SECTION_GENERIC_LOOKUP_TABLES);
        foreach (var (rootUniqueId, table) in tablesByRootUniqueId) {
            var categoriesElement = new XElement(GenericLookupTableParser.CATEGORIES);
            categoriesElement.SetAttributeValue(GenericLookupTableParser.FOR, rootUniqueId);
            foreach (var category in table.Categories) {
                if (string.IsNullOrEmpty(category.Name)) continue;
                var categoryElement = new XElement(category.Name);
                if (!string.IsNullOrEmpty(category.DisplayName)) {
                    categoryElement.SetAttributeValue(BasicNodeOptions.DisplayName.AttributeName, category.DisplayName);
                }
                foreach (var (uniqueId, value) in category.HardCodedKeyValues) {
                    var keyElement = new XElement(GenericLookupTableParser.KEY);
                    keyElement.SetAttributeValue(GenericLookupTableParser.FOR, uniqueId);
                    keyElement.SetAttributeValue(GenericLookupTableParser.KEY_VALUE, value);
                    categoryElement.Add(keyElement);
                }
                categoriesElement.Add(categoryElement);
            }
            if (categoriesElement.HasElements) {
                section.Add(categoriesElement);
            }
        }
        return section.HasElements ? section : null;
    }
}

/// <summary>
/// 汎用参照テーブルの1カテゴリ分のデータ
/// </summary>
public class EditingGenericLookupTableCategory {
    /// <summary>カテゴリ名（XML要素名） 例: "Countries"</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    /// <summary>表示用名称 例: "国・地域区分"</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    /// <summary>ハードコードされるキーの値: メンバーの UniqueId → Value のマッピング</summary>
    [JsonPropertyName("hardCodedKeyValues")]
    public Dictionary<string, string> HardCodedKeyValues { get; set; } = new();
}
