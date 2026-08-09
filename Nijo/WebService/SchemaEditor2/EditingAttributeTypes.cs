using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Nijo.SchemaParsing;

namespace Nijo.WebService.SchemaEditor2;

/// <summary>
/// XML属性の値とJSON値の間の型変換ルール。
/// どの属性がBoolean型・数値型かは、既定の属性定義（<see cref="NodeOption"/>）と
/// カスタム属性定義（<see cref="NijoXmlCustomAttribute"/>）から決まる。
/// それ以外の属性名（静的区分値の key 属性など、既定の属性定義に載っていないもの）は文字列として扱う。
/// </summary>
internal class EditingAttributeTypes {
    internal EditingAttributeTypes(SchemaParseRule rule, IEnumerable<NijoXmlCustomAttribute> customAttributes) {
        _booleanAttributeNames = rule.NodeOptions
            .Where(opt => opt.Type == E_NodeOptionType.Boolean)
            .Select(opt => opt.AttributeName)
            .ToHashSet();
        _numberAttributeNames = rule.NodeOptions
            .Where(opt => opt.Type == E_NodeOptionType.Integer)
            .Select(opt => opt.AttributeName)
            .ToHashSet();

        foreach (var customAttribute in customAttributes) {
            if (string.IsNullOrEmpty(customAttribute.UniqueId)) continue;
            if (customAttribute.Type == NijoXmlCustomAttribute.E_Type.Boolean) {
                _booleanAttributeNames.Add(customAttribute.UniqueId);
            } else if (customAttribute.Type == NijoXmlCustomAttribute.E_Type.Decimal) {
                _numberAttributeNames.Add(customAttribute.UniqueId);
            }
        }
    }
    private readonly HashSet<string> _booleanAttributeNames;
    private readonly HashSet<string> _numberAttributeNames;

    /// <summary>
    /// XML上の属性値をJSON値に変換する。この属性がXML要素に存在する場合にのみ呼び出される想定。
    /// </summary>
    internal JsonNode? ToJsonValue(string attributeName, string xmlValue) {
        if (_booleanAttributeNames.Contains(attributeName)) {
            return xmlValue.Equals("True", System.StringComparison.OrdinalIgnoreCase);
        }
        if (_numberAttributeNames.Contains(attributeName) && decimal.TryParse(xmlValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)) {
            return number;
        }
        return xmlValue;
    }

    /// <summary>
    /// JSON値をXML属性値に変換する。属性を出力すべきでない場合（false・空文字・未指定）はnullを返す。
    /// </summary>
    internal string? ToXmlValue(string attributeName, JsonNode? value) {
        if (value == null) return null;

        if (_booleanAttributeNames.Contains(attributeName)) {
            return value.GetValue<bool>() ? "True" : null;
        }
        if (_numberAttributeNames.Contains(attributeName)) {
            return value.GetValue<decimal>().ToString(CultureInfo.InvariantCulture);
        }

        var str = value.GetValue<string>();
        return string.IsNullOrWhiteSpace(str) ? null : str;
    }

    /// <summary>
    /// 属性を名前の昇順（Ordinal）でXML要素にセットする。
    /// 保存の度に無関係な属性順の差分が生じないよう、常に同じ順序で書き込む。
    /// </summary>
    internal static void ApplyAttributesInOrder(XElement element, IEnumerable<KeyValuePair<string, string>> attributePairs) {
        foreach (var (name, value) in attributePairs.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            element.SetAttributeValue(name, value);
        }
    }
}
