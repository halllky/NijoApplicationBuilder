using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// nijo.xml のXML要素1個に定義できる属性の型定義。
/// ほぼ <see cref="Nijo.SchemaParsing.NodeOption"/> とだいたい同じ。
/// </summary>
public class XmlAttributeDef {
    [JsonPropertyName("attributeName")]
    public string AttributeName { get; set; } = "";
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    [JsonPropertyName("availableElements")]
    public List<AvailableElement> AvailableElements { get; set; } = new List<AvailableElement>();
    [JsonPropertyName("type"), JsonConverter(typeof(JsonStringEnumConverter))]
    public E_NodeOptionType Type { get; set; }
    [JsonPropertyName("typeEnumValues")]
    public string[]? TypeEnumValues { get; set; }

    public class AvailableElement {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";
        [JsonPropertyName("nodeType")]
        public string NodeType { get; set; } = "";
    }

    internal static List<XmlAttributeDef> FromSchemaParseRule(SchemaParseRule rule) {
        return rule.NodeOptions.Select(opt => {
            // 各モデル × 各ノード種別の組み合わせについて、この属性が使用可能かチェックする
            var availableElements = new List<AvailableElement>();
            foreach (var model in rule.Models) {
                foreach (var nodeType in System.Enum.GetValues<E_NodeType>()) {
                    if (opt.IsAvailable(model, nodeType)) {
                        availableElements.Add(new AvailableElement {
                            Model = model.SchemaName,
                            NodeType = nodeType.ToString(),
                        });
                    }
                }
            }

            return new XmlAttributeDef {
                AttributeName = opt.AttributeName,
                DisplayName = opt.DisplayName,
                AvailableElements = availableElements,
                Type = opt.Type,
                TypeEnumValues = opt.TypeEnumValues,
            };
        }).ToList();
    }
}
