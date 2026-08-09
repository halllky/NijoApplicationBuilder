using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Nijo.WebService.SchemaEditor2;

/// <summary>
/// ユニーク制約1件分。nijo.xml 上では UniqueConstraints 属性の中に
/// セミコロン区切りで複数件、カンマ区切りで対象メンバーの UniqueId が並ぶ形で記述される。
/// </summary>
public class EditingUniqueConstraint {
    [JsonPropertyName("memberUniqueIds")]
    public List<string> MemberUniqueIds { get; set; } = [];

    /// <summary>
    /// UniqueConstraints 属性の値（例: "xxxx,yyyy;zzzz;"）を解析する。
    /// </summary>
    internal static List<EditingUniqueConstraint> FromAttributeValue(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) return [];

        return raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(constraintText => new EditingUniqueConstraint {
                MemberUniqueIds = constraintText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList(),
            })
            .ToList();
    }

    /// <summary>
    /// UniqueConstraints 属性の値に変換する。1件以上あれば末尾にセミコロンを付与する。
    /// </summary>
    internal static string? ToAttributeValue(IReadOnlyList<EditingUniqueConstraint> constraints) {
        var nonEmpty = constraints.Where(c => c.MemberUniqueIds.Count > 0).ToList();
        if (nonEmpty.Count == 0) return null;

        return string.Join(";", nonEmpty.Select(c => string.Join(",", c.MemberUniqueIds))) + ";";
    }
}
