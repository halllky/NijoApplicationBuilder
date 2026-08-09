using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Nijo.CodeGenerating;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// スキーマ編集画面が必要とする、プロジェクトに依存しない固定ルール。
/// 編集対象ではないため、編集中のプロジェクトの内容（<see cref="EditingProject"/>）とは別に取得する。
/// </summary>
public class SchemaEditorRule {
    [JsonPropertyName("models")]
    public List<ModelDef> Models { get; set; } = [];
    [JsonPropertyName("attributeDefs")]
    public List<AttributeDef> AttributeDefs { get; set; } = [];
    [JsonPropertyName("valueMemberTypes")]
    public List<ValueMemberTypeDef> ValueMemberTypes { get; set; } = [];
    [JsonPropertyName("projectOptionPropertyInfos")]
    public List<ProjectOptionPropertyInfo> ProjectOptionPropertyInfos { get; set; } = [];

    internal static SchemaEditorRule FromSchemaParseRule(SchemaParseRule rule) {
        return new SchemaEditorRule {
            Models = rule.Models.Select(m => new ModelDef { SchemaName = m.SchemaName }).ToList(),
            AttributeDefs = AttributeDef.FromSchemaParseRule(rule),
            ValueMemberTypes = rule.ValueMemberTypes.Select(vmt => new ValueMemberTypeDef {
                SchemaTypeName = vmt.SchemaTypeName,
                TypeDisplayName = vmt.DisplayName,
            }).ToList(),
            ProjectOptionPropertyInfos = GeneratedProjectOptions.GetPropertyInfos().ToList(),
        };
    }
}

/// <summary>ルート集約の種類（data-model, query-model, enum など）。</summary>
public class ModelDef {
    [JsonPropertyName("schemaName")]
    public string SchemaName { get; set; } = "";
}

/// <summary><see cref="Nijo.ImmutableSchema.IValueMemberType"/> のスキーマ定義編集画面GUI上でのデータ構造。</summary>
public class ValueMemberTypeDef {
    [JsonPropertyName("schemaTypeName")]
    public string SchemaTypeName { get; set; } = "";
    [JsonPropertyName("typeDisplayName")]
    public string TypeDisplayName { get; set; } = "";
}

/// <summary>
/// nijo.xml のXML要素1個に定義できる属性の型定義。ほぼ <see cref="Nijo.SchemaParsing.NodeOption"/> と同じ。
/// </summary>
public class AttributeDef {
    [JsonPropertyName("attributeName")]
    public string AttributeName { get; set; } = "";
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    [JsonPropertyName("availableElements")]
    public List<AvailableElement> AvailableElements { get; set; } = [];
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

    internal static List<AttributeDef> FromSchemaParseRule(SchemaParseRule rule) {
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

            return new AttributeDef {
                AttributeName = opt.AttributeName,
                DisplayName = opt.DisplayName,
                AvailableElements = availableElements,
                Type = opt.Type,
                TypeEnumValues = opt.TypeEnumValues,
            };
        }).ToList();
    }
}
