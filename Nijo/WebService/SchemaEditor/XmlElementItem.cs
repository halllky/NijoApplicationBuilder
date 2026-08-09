using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// XML要素1個分と対応するデータ型。
/// <see cref="RootAggregateXmlTree"/> がこの型のフラットな配列を保持し、
/// <see cref="Indent"/> によって親子関係を表す。
/// </summary>
public class XmlElementItem {
    [JsonPropertyName("uniqueId")]
    public string UniqueId { get; set; } = "";
    [JsonPropertyName("indent")]
    public int Indent { get; set; } = 0;
    [JsonPropertyName("localName")]
    public string? LocalName { get; set; } = null;
    [JsonPropertyName("value")]
    public string? Value { get; set; } = null;
    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = [];
    [JsonPropertyName("comment")]
    public string? Comment { get; set; } = null;
}
