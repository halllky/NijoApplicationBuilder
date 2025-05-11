using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nijo.ImmutableSchema;
using Nijo.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Nijo.Ui;

// HTTPリクエスト・レスポンスに使うため、下記で定義されている各種データクラスと合わせたデータ構造を定義する
// haldoc\Nijo.ApplicationTemplate.Ver1\react\src\debug-rooms\スキーマ定義編集UIの試作\types.ts

/// <summary>
/// アプリケーション全体の状態
/// </summary>
public class ApplicationState {
    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; set; } = "";
    [JsonPropertyName("xmlElementTrees")]
    public List<ModelPageForm> XmlElementTrees { get; set; } = [];
    [JsonPropertyName("attributeDefs")]
    public List<XmlElementAttribute> AttributeDefs { get; set; } = [];
    [JsonPropertyName("valueMemberTypes")]
    public List<ValueMemberType> ValueMemberTypes { get; set; } = [];

    /// <summary>
    /// 新しい <see cref="XDocument"/> インスタンスを構築して返す。
    /// ValueMemberやAttributeDefsは、暫定的に、画面上で編集できないものとし、
    /// <see cref="SchemaParseRule.Default"/> から取得する。
    /// </summary>
    internal bool TryConvertToXDocument(XDocument original, ICollection<string> errors, [NotNullWhen(true)] out XDocument? xDocument) {

        xDocument = new XDocument(original);
        if (xDocument.Root == null) {
            errors.Add("XMLにルート要素がありません");
            xDocument = null;
            return false;
        }

        xDocument.Root.RemoveNodes();

        for (int i = 0; i < XmlElementTrees.Count; i++) {
            var aggregateTree = XmlElementTrees[i];
            var logName = aggregateTree.XmlElements.Count > 0
                ? $"{aggregateTree.XmlElements[0].LocalName}のツリー"
                : $"第{i + 1}番目の集約ツリー";
            if (XmlElementItem.TryConvertToRootAggregateXElement(
                aggregateTree.XmlElements,
                error => errors.Add($"{logName}: {error}"),
                out var rootAggregate,
                out var commentToRootAggregate)) {

                if (commentToRootAggregate != null) xDocument.Root.Add(commentToRootAggregate);
                xDocument.Root.Add(rootAggregate);
            }
        }
        if (errors.Count > 0) {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Model定義画面のデータ型定義
/// </summary>
public class ModelPageForm {
    [JsonPropertyName("xmlElements")]
    public List<XmlElementItem> XmlElements { get; set; } = [];
}

/// <summary>
/// <see cref="ImmutableSchema.IValueMemberType"/> に同じ。
/// </summary>
public class ValueMemberType {
    [JsonPropertyName("schemaTypeName")]
    public string SchemaTypeName { get; set; } = "";
    [JsonPropertyName("typeDisplayName")]
    public string TypeDisplayName { get; set; } = "";

    internal static List<ValueMemberType> FromSchemaParseRule(SchemaParseRule rule) {
        return rule.ValueMemberTypes.Select(vmt => new ValueMemberType {
            SchemaTypeName = vmt.SchemaTypeName,
            TypeDisplayName = vmt.DisplayName,
        }).ToList();
    }
}

/// <summary>
/// XML要素1個分と対応するデータ型
/// </summary>
public class XmlElementItem {
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
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

    /// <summary>
    /// <see cref="XElement"/> を <see cref="XmlElementItem"/> のリストに変換する。
    /// </summary>
    /// <param name="element">ルート集約</param>
    public static IEnumerable<XmlElementItem> FromXElement(XElement element) {
        return EnumerateRecursive(element);

        static IEnumerable<XmlElementItem> EnumerateRecursive(XElement element) {
            string? comment;
            if (element.PreviousNode is XComment xComment && !string.IsNullOrWhiteSpace(xComment.Value)) {
                comment = xComment.Value;
            } else {
                comment = null;
            }
            yield return new XmlElementItem {
                Id = Guid.NewGuid().ToString(),
                Indent = element.Ancestors().Count() - 1,
                LocalName = element.Name.LocalName,
                Value = element.Value,
                Attributes = element.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value),
                Comment = comment,
            };
            foreach (var child in element.Elements()) {
                foreach (var item in EnumerateRecursive(child)) {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// <see cref="XmlElementItem"/> のリストを <see cref="XElement"/> のリストに変換する。
    /// ルート集約の塊ごとに変換する想定のため、引数のリストの先頭の要素のインデントは0、以降のインデントは1以上であることを前提とする。
    /// </summary>
    public static bool TryConvertToRootAggregateXElement(
        IReadOnlyList<XmlElementItem> items,
        Action<string> logError,
        [NotNullWhen(true)] out XElement? rootAggregate,
        out XComment? commentToRootAggregate) {

        if (items.Count == 0) {
            logError("要素がありません");
            rootAggregate = null;
            commentToRootAggregate = null;
            return false;
        }
        if (items[0].Indent != 0) {
            logError($"先頭の要素のインデントが0であるべきところ{items[0].Indent}です");
            rootAggregate = null;
            commentToRootAggregate = null;
            return false;
        }

        var stack = new Stack<(int Indent, XElement Element, XComment? XComment)>();
        var previous = ((int Indent, XElement Element, XComment? XComment)?)null;
        for (int i = 0; i < items.Count; i++) {
            var item = items[i];

            // XElementへの変換
            if (item.LocalName == null) {
                logError($"{i + 1}番目の要素の名前が空です");
                rootAggregate = null;
                commentToRootAggregate = null;
                return false;
            }
            var xComment = string.IsNullOrWhiteSpace(item.Comment)
                ? null
                : new XComment(item.Comment);
            var xElement = new XElement(item.LocalName);
            if (!string.IsNullOrWhiteSpace(item.Value)) xElement.SetValue(item.Value);
            foreach (var attribute in item.Attributes) {
                if (string.IsNullOrWhiteSpace(attribute.Value)) continue;
                xElement.SetAttributeValue(attribute.Key, attribute.Value);
            }

            // 前の要素が空ならば element はルート集約
            if (previous == null) {
                previous = (item.Indent, xElement, xComment);
                stack.Push(previous.Value);
            }

            // ルートでないのにインデントが0ならばエラー
            else if (item.Indent == 0) {
                logError($"{item.LocalName}: ルートでないのにインデントが0です");
                rootAggregate = null;
                commentToRootAggregate = null;
                return false;
            }

            // itemのインデントが前の要素のインデントと同じなら、elementはstackの最上位の要素の子
            else if (item.Indent == previous.Value.Indent) {
                previous = (item.Indent, xElement, xComment);

                var parent = stack.Peek();
                if (xComment != null) parent.Element.Add(xComment);
                parent.Element.Add(xElement);
            }

            // itemのインデントが前の要素より深くなっていたら、前の要素が stack に積まれ、elementはその子
            else if (item.Indent > previous.Value.Indent) {
                stack.Push(previous.Value);
                previous = (item.Indent, xElement, xComment);

                var parent = stack.Peek();
                if (xComment != null) parent.Element.Add(xComment);
                parent.Element.Add(xElement);
            }

            // itemのインデントが前の要素より浅くなっていたら、stackのうちインデントが浅いものが出現するまで pop して、そのうち最上位の要素の子にする
            else {
                while (stack.Peek().Indent >= item.Indent) {
                    stack.Pop();
                }
                previous = (item.Indent, xElement, xComment);

                var parent = stack.Peek();
                if (xComment != null) parent.Element.Add(xComment);
                parent.Element.Add(xElement);
            }
        }

        var root = stack.Reverse().First();
        rootAggregate = root.Element;
        commentToRootAggregate = root.XComment;
        return true;
    }
}

/// <summary>
/// XML要素の属性の種類定義
/// </summary>
public class XmlElementAttribute {
    [JsonPropertyName("attributeName")]
    public string AttributeName { get; set; } = "";
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    [JsonPropertyName("availableModels")]
    public List<string> AvailableModels { get; set; } = new List<string>();

    internal static List<XmlElementAttribute> FromSchemaParseRule(SchemaParseRule rule) {
        return rule.NodeOptions.Select(ad => {
            // 各モデルについて、この属性が使用可能かチェックする
            var availableModels = new List<string>();
            foreach (var model in rule.Models) {
                // NodeOption.IsAvailableModelMembersがnullの場合は常にtrueと同じ
                if (ad.IsAvailableModelMembers == null || ad.IsAvailableModelMembers(model)) {
                    availableModels.Add(model.SchemaName);
                }
            }

            return new XmlElementAttribute {
                AttributeName = ad.AttributeName,
                DisplayName = ad.DisplayName,
                AvailableModels = availableModels,
            };
        }).ToList();
    }
}
