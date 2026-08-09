using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor;

/// <summary>
/// 集約の子孫要素（Child, Children, ValueMember, Ref）1個分。
/// 同じルート集約に属するメンバー同士の親子関係は <see cref="Indent"/> によって表す。
/// </summary>
public class EditingMember {
    [JsonPropertyName("uniqueId")]
    public string UniqueId { get; set; } = "";
    /// <summary>親子関係を表すインデントレベル。1以上。</summary>
    [JsonPropertyName("indent")]
    public int Indent { get; set; } = 1;
    [JsonPropertyName("physicalName")]
    public string? PhysicalName { get; set; }
    /// <summary>XML要素のテキスト値。</summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
    [JsonPropertyName("type")]
    public EditingMemberType Type { get; set; } = new();
    /// <summary>Type・UniqueId・UniqueConstraints を除いた属性。</summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonNode?> Attributes { get; set; } = new();
    [JsonPropertyName("uniqueConstraints")]
    public List<EditingUniqueConstraint> UniqueConstraints { get; set; } = [];

    /// <summary>
    /// メンバーを表すXML要素1個から <see cref="EditingMember"/> を作る（子孫は含まない）。
    /// </summary>
    internal static EditingMember FromXElement(XElement element, int indent, EditingAttributeTypes attributeTypes, IReadOnlySet<string> knownValueTypeNames) {
        var attributes = new Dictionary<string, JsonNode?>();
        var uniqueConstraints = new List<EditingUniqueConstraint>();
        foreach (var attr in element.Attributes()) {
            var name = attr.Name.LocalName;
            if (name == SchemaParseContext.ATTR_NODE_TYPE || name == SchemaParseContext.ATTR_UNIQUE_ID) continue;
            if (name == BasicNodeOptions.UniqueConstraints.AttributeName) {
                uniqueConstraints = EditingUniqueConstraint.FromAttributeValue(attr.Value);
                continue;
            }
            attributes[name] = attributeTypes.ToJsonValue(name, attr.Value);
        }

        return new EditingMember {
            UniqueId = element.Attribute(SchemaParseContext.ATTR_UNIQUE_ID)?.Value ?? Guid.NewGuid().ToString(),
            Indent = indent,
            PhysicalName = element.Name.LocalName,
            Value = element.Value,
            Comment = element.PreviousNode is XComment xComment && !string.IsNullOrWhiteSpace(xComment.Value)
                ? xComment.Value
                : null,
            Type = EditingMemberType.FromAttributeValue(element.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value, knownValueTypeNames),
            Attributes = attributes,
            UniqueConstraints = uniqueConstraints,
        };
    }

    /// <summary>
    /// このメンバー1個分の <see cref="XElement"/> を構築する（子孫要素は呼び出し側が付け足す）。
    /// </summary>
    internal bool TryToXElement(
        EditingAttributeTypes attributeTypes,
        Action<string> logError,
        [NotNullWhen(true)] out XElement? xElement,
        out XComment? xComment) {

        var trimmedName = PhysicalName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName)) {
            logError("メンバーの名前が空です");
            xElement = null;
            xComment = null;
            return false;
        }

        try {
            xElement = new XElement(trimmedName);
        } catch (XmlException ex) {
            logError($"'{trimmedName}': XML要素として不正です: {ex.Message}");
            xElement = null;
            xComment = null;
            return false;
        }
        xComment = string.IsNullOrWhiteSpace(Comment) ? null : new XComment(Comment);

        if (!string.IsNullOrWhiteSpace(Value)) xElement.SetValue(Value);

        var attributePairs = new List<KeyValuePair<string, string>>();

        var typeValue = Type.ToAttributeValue();
        if (!string.IsNullOrWhiteSpace(typeValue)) attributePairs.Add(new(SchemaParseContext.ATTR_NODE_TYPE, typeValue));

        foreach (var (name, value) in Attributes) {
            var xmlValue = attributeTypes.ToXmlValue(name, value);
            if (xmlValue != null) attributePairs.Add(new(name, xmlValue));
        }

        var uniqueConstraintsValue = EditingUniqueConstraint.ToAttributeValue(UniqueConstraints);
        if (uniqueConstraintsValue != null) attributePairs.Add(new(BasicNodeOptions.UniqueConstraints.AttributeName, uniqueConstraintsValue));

        attributePairs.Add(new(SchemaParseContext.ATTR_UNIQUE_ID, UniqueId));

        EditingAttributeTypes.ApplyAttributesInOrder(xElement, attributePairs);
        return true;
    }
}

/// <summary>
/// メンバーの種類（Type属性の解釈結果）。nijo.xml の書式（"ref-to:A/B" 等）を判別可能ユニオンに変換したもの。
/// </summary>
public class EditingMemberType {
    internal const string KIND_CHILD = "child";
    internal const string KIND_CHILDREN = "children";
    internal const string KIND_REF_TO = "ref-to";
    internal const string KIND_VALUE = "value";
    internal const string KIND_UNKNOWN = "unknown";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = KIND_UNKNOWN;
    /// <summary>Kindが"value"の場合の値の種類名（word, decimal, 静的区分名, 値オブジェクト名など）</summary>
    [JsonPropertyName("valueTypeName")]
    public string? ValueTypeName { get; set; }
    /// <summary>Kindが"ref-to"の場合の参照先パス。"ref-to:A/B" の "A/B" 部分をスラッシュで分割したもの。</summary>
    [JsonPropertyName("refToPath")]
    public List<string>? RefToPath { get; set; }
    /// <summary>Kindが"unknown"の場合の元のType属性値。</summary>
    [JsonPropertyName("rawValue")]
    public string? RawValue { get; set; }

    /// <summary>
    /// Type属性の値を解釈する。
    /// </summary>
    /// <param name="knownValueTypeNames">このプロジェクトで定義済みの値の種類名（既定の値メンバー型・静的区分名・値オブジェクト名）の集合。</param>
    internal static EditingMemberType FromAttributeValue(string? typeAttrValue, IReadOnlySet<string> knownValueTypeNames) {
        if (string.IsNullOrEmpty(typeAttrValue)) {
            return new EditingMemberType { Kind = KIND_UNKNOWN };
        }
        if (typeAttrValue == SchemaParseContext.NODE_TYPE_CHILD) {
            return new EditingMemberType { Kind = KIND_CHILD };
        }
        if (typeAttrValue == SchemaParseContext.NODE_TYPE_CHILDREN) {
            return new EditingMemberType { Kind = KIND_CHILDREN };
        }

        const string refToPrefix = SchemaParseContext.NODE_TYPE_REFTO + ":";
        if (typeAttrValue.StartsWith(refToPrefix, StringComparison.Ordinal)) {
            return new EditingMemberType {
                Kind = KIND_REF_TO,
                RefToPath = typeAttrValue[refToPrefix.Length..].Split('/').ToList(),
            };
        }

        if (knownValueTypeNames.Contains(typeAttrValue)) {
            return new EditingMemberType { Kind = KIND_VALUE, ValueTypeName = typeAttrValue };
        }

        return new EditingMemberType { Kind = KIND_UNKNOWN, RawValue = typeAttrValue };
    }

    /// <summary>Type属性の値に復元する。復元できない場合はnull。</summary>
    internal string? ToAttributeValue() {
        return Kind switch {
            KIND_CHILD => SchemaParseContext.NODE_TYPE_CHILD,
            KIND_CHILDREN => SchemaParseContext.NODE_TYPE_CHILDREN,
            KIND_REF_TO when RefToPath is { Count: > 0 } => $"{SchemaParseContext.NODE_TYPE_REFTO}:{string.Join("/", RefToPath)}",
            KIND_VALUE => ValueTypeName,
            _ => RawValue,
        };
    }
}
