using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// 汎用参照テーブル1個分のカテゴリ定義データ（GUI編集用）
/// </summary>
public class GenericLookupTableCategories {
    /// <summary>対象ルート集約のUniqueId</summary>
    [JsonPropertyName("for")]
    public string For { get; set; } = "";
    /// <summary>カテゴリ一覧</summary>
    [JsonPropertyName("categories")]
    public List<GenericLookupTableCategory> Categories { get; set; } = [];

    /// <summary>
    /// nijo.xml の GenericLookupTableCategories セクションを読み込んで返す。
    /// </summary>
    internal static List<GenericLookupTableCategories> FromXDocument(XDocument xDocument) {
        var result = new List<GenericLookupTableCategories>();
        var section = xDocument.Root?.Element(SchemaParseContext.SECTION_GENERIC_LOOKUP_TABLES);
        if (section == null) return result;

        foreach (var categoriesElement in section.Elements(GenericLookupTableParser.CATEGORIES)) {
            var forAttr = categoriesElement.Attribute(GenericLookupTableParser.FOR)?.Value;
            if (string.IsNullOrEmpty(forAttr)) continue;

            var data = new GenericLookupTableCategories { For = forAttr };
            foreach (var categoryElement in categoriesElement.Elements()) {
                var category = new GenericLookupTableCategory {
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
                data.Categories.Add(category);
            }
            result.Add(data);
        }
        return result;
    }

    /// <summary>
    /// 汎用参照テーブルのカテゴリセクションをXML要素に変換する。中身が空ならnullを返す。
    /// </summary>
    internal static XElement? ToXElement(IEnumerable<GenericLookupTableCategories> categoriesList) {
        var genericLookupSection = new XElement(SchemaParseContext.SECTION_GENERIC_LOOKUP_TABLES);
        foreach (var data in categoriesList) {
            if (string.IsNullOrEmpty(data.For)) continue;
            var categoriesElement = new XElement(GenericLookupTableParser.CATEGORIES);
            categoriesElement.SetAttributeValue(GenericLookupTableParser.FOR, data.For);
            foreach (var category in data.Categories) {
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
                genericLookupSection.Add(categoriesElement);
            }
        }
        return genericLookupSection.HasElements ? genericLookupSection : null;
    }
}

/// <summary>
/// 汎用参照テーブルの1カテゴリ分のデータ（GUI編集用）
/// </summary>
public class GenericLookupTableCategory {
    /// <summary>カテゴリ名（XML要素名）例: "Countries"</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    /// <summary>表示用名称 例: "国・地域区分"</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    /// <summary>ハードコードされるキーの値: UniqueId → Value のマッピング</summary>
    [JsonPropertyName("hardCodedKeyValues")]
    public Dictionary<string, string> HardCodedKeyValues { get; set; } = new();
}
