using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor2;

/// <summary>
/// ルート集約1個分。データ構造・コマンド・静的区分・値オブジェクト・定数のいずれも、
/// nijo.xml 上ではこの形（ルート要素1個＋子孫要素の並び）で表される。
/// </summary>
public class EditingRootAggregate {
    [JsonPropertyName("uniqueId")]
    public string UniqueId { get; set; } = "";
    /// <summary>XML要素のローカル名。</summary>
    [JsonPropertyName("physicalName")]
    public string? PhysicalName { get; set; }
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
    /// <summary>Type属性の値（data-model, query-model, enum など）。</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    /// <summary>Type・UniqueId・UniqueConstraints を除いた属性。</summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonNode?> Attributes { get; set; } = new();
    [JsonPropertyName("uniqueConstraints")]
    public List<EditingUniqueConstraint> UniqueConstraints { get; set; } = [];
    /// <summary>IsGenericLookupTable が指定されたルート集約のみ値を持つ。</summary>
    [JsonPropertyName("genericLookupTable")]
    public EditingGenericLookupTable? GenericLookupTable { get; set; }
    /// <summary>子孫要素。Indent は 1 以上。</summary>
    [JsonPropertyName("members")]
    public List<EditingMember> Members { get; set; } = [];

    /// <summary>
    /// ルート集約のXML要素から <see cref="EditingRootAggregate"/> を作る。
    /// </summary>
    internal static EditingRootAggregate FromXElement(
        XElement rootElement,
        EditingAttributeTypes attributeTypes,
        IReadOnlySet<string> knownValueTypeNames,
        EditingGenericLookupTable? genericLookupTable) {

        var attributes = new Dictionary<string, JsonNode?>();
        var uniqueConstraints = new List<EditingUniqueConstraint>();
        foreach (var attr in rootElement.Attributes()) {
            var name = attr.Name.LocalName;
            if (name == SchemaParseContext.ATTR_NODE_TYPE || name == SchemaParseContext.ATTR_UNIQUE_ID) continue;
            if (name == BasicNodeOptions.UniqueConstraints.AttributeName) {
                uniqueConstraints = EditingUniqueConstraint.FromAttributeValue(attr.Value);
                continue;
            }
            attributes[name] = attributeTypes.ToJsonValue(name, attr.Value);
        }

        var members = new List<EditingMember>();
        void AddMembersRecursively(XElement element, int indent) {
            foreach (var child in element.Elements()) {
                members.Add(EditingMember.FromXElement(child, indent, attributeTypes, knownValueTypeNames));
                AddMembersRecursively(child, indent + 1);
            }
        }
        AddMembersRecursively(rootElement, 1);

        return new EditingRootAggregate {
            UniqueId = rootElement.Attribute(SchemaParseContext.ATTR_UNIQUE_ID)?.Value ?? Guid.NewGuid().ToString(),
            PhysicalName = rootElement.Name.LocalName,
            Comment = rootElement.PreviousNode is XComment xComment && !string.IsNullOrWhiteSpace(xComment.Value)
                ? xComment.Value
                : null,
            Model = rootElement.Attribute(SchemaParseContext.ATTR_NODE_TYPE)?.Value,
            Attributes = attributes,
            UniqueConstraints = uniqueConstraints,
            GenericLookupTable = genericLookupTable,
            Members = members,
        };
    }

    /// <summary>
    /// この <see cref="EditingRootAggregate"/> を <see cref="XElement"/> に変換する。
    /// <see cref="Members"/> は先頭ほど親、Indent の増減で親子関係を復元する（インデントスタック法）。
    /// </summary>
    internal bool TryToXElement(
        EditingAttributeTypes attributeTypes,
        Action<string> logError,
        Action<XElement, string> onXmlElementCreated,
        [NotNullWhen(true)] out XElement? rootElement,
        out XComment? commentToRoot) {

        rootElement = null;
        commentToRoot = null;

        var trimmedName = PhysicalName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName)) {
            logError("ルート集約の名前が空です");
            return false;
        }

        XElement xRoot;
        try {
            xRoot = new XElement(trimmedName);
        } catch (XmlException ex) {
            logError($"'{trimmedName}': XML要素として不正です: {ex.Message}");
            return false;
        }
        onXmlElementCreated(xRoot, UniqueId);

        var rootAttributePairs = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrWhiteSpace(Model)) rootAttributePairs.Add(new(SchemaParseContext.ATTR_NODE_TYPE, Model));
        foreach (var (name, value) in Attributes) {
            var xmlValue = attributeTypes.ToXmlValue(name, value);
            if (xmlValue != null) rootAttributePairs.Add(new(name, xmlValue));
        }
        var uniqueConstraintsValue = EditingUniqueConstraint.ToAttributeValue(UniqueConstraints);
        if (uniqueConstraintsValue != null) rootAttributePairs.Add(new(BasicNodeOptions.UniqueConstraints.AttributeName, uniqueConstraintsValue));
        rootAttributePairs.Add(new(SchemaParseContext.ATTR_UNIQUE_ID, UniqueId));
        EditingAttributeTypes.ApplyAttributesInOrder(xRoot, rootAttributePairs);

        // インデントスタック法でメンバーの親子関係を復元する。
        // スタックの各段は「そのインデント以下の子孫を受け入れられる直近の要素」を表す。
        var stack = new Stack<(int Indent, XElement Element)>();
        stack.Push((0, xRoot));

        foreach (var member in Members) {
            if (member.Indent < 1) {
                logError($"'{member.PhysicalName}': メンバーのインデントは1以上である必要があります");
                rootElement = null;
                commentToRoot = null;
                return false;
            }
            if (!member.TryToXElement(attributeTypes, logError, out var xMember, out var xMemberComment)) {
                rootElement = null;
                commentToRoot = null;
                return false;
            }
            onXmlElementCreated(xMember, member.UniqueId);

            while (stack.Peek().Indent >= member.Indent) {
                stack.Pop();
            }
            var parent = stack.Peek().Element;
            if (xMemberComment != null) parent.Add(xMemberComment);
            parent.Add(xMember);
            stack.Push((member.Indent, xMember));
        }

        rootElement = xRoot;
        commentToRoot = string.IsNullOrWhiteSpace(Comment) ? null : new XComment(Comment);
        return true;
    }
}
