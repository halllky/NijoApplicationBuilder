using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.SchemaParsing;

/// <summary>
/// XMLスキーマ解釈ルール
/// </summary>
public class SchemaParseRule {
    /// <summary>
    /// ルート集約の種類
    /// </summary>
    public required IModel[] Models { get; init; }
    /// <summary>
    /// 値メンバーの種類
    /// </summary>
    public required IValueMemberType[] ValueMemberTypes { get; init; }
    /// <summary>
    /// オプション項目の種類
    /// </summary>
    public required NodeOption[] NodeOptions { get; init; }


    /// <summary>
    /// 既定の型解釈ルール
    /// </summary>
    public static SchemaParseRule Default() {
        var models = new IModel[] {
            new DataModel(),
            new QueryModel(),
            new CommandModel(),
            new StaticEnumModel(),
            new ValueObjectModel(),
        };
        var valueMemberTypes = new IValueMemberType[] {
            new ValueMemberTypes.Word(),
            new ValueMemberTypes.IntMember(),
            new ValueMemberTypes.DateTimeMember(),
            new ValueMemberTypes.DateMember(),
            new ValueMemberTypes.YearMonthMember(),
            new ValueMemberTypes.YearMember(),
            new ValueMemberTypes.Description(),
            new ValueMemberTypes.DecimalMember(),
            new ValueMemberTypes.BoolMember(),
            new ValueMemberTypes.ByteArrayMember(),
        };
        var nodeOptions = new NodeOption[] {
            BasicNodeOptions.DisplayName,
            BasicNodeOptions.DbName,
            BasicNodeOptions.LatinName,
            BasicNodeOptions.IsKey,
            BasicNodeOptions.IsRequired,
            BasicNodeOptions.GenerateDefaultQueryModel,
            BasicNodeOptions.GenerateBatchUpdateCommand,
            BasicNodeOptions.IsReadOnly,
            BasicNodeOptions.HasLifeCycle,
            BasicNodeOptions.MaxLength,
            BasicNodeOptions.CharacterType,
            BasicNodeOptions.TotalDigit,
            BasicNodeOptions.DecimalPlace,
            BasicNodeOptions.SequenceName,
        };
        return new() {
            Models = models,
            ValueMemberTypes = valueMemberTypes,
            NodeOptions = nodeOptions,
        };
    }

    /// <summary>
    /// このルールの整合性を検証します。
    /// </summary>
    /// <exception cref="InvalidOperationException">ルールに問題がある場合</exception>
    public void ThrowIfInvalid() {
        // スキーマ定義名重複チェック
        var appearedName = new HashSet<string>();
        var duplicates = new HashSet<string>();
        foreach (var name in Models.Select(m => m.SchemaName).Concat(ValueMemberTypes.Select(t => t.SchemaTypeName))) {
            if (appearedName.Contains(name)) {
                duplicates.Add(name);
            } else {
                appearedName.Add(name);
            }
        }
        if (duplicates.Count > 0) {
            throw new InvalidOperationException($"型名 {string.Join(", ", duplicates)} が重複しています。");
        }

        // オプション属性のキー重複チェック
        var groupedOptions = NodeOptions
            .GroupBy(opt => opt.AttributeName)
            .Where(group => group.Count() >= 2)
            .ToArray();
        if (groupedOptions.Length > 0) {
            throw new InvalidOperationException($"オプション属性名 {groupedOptions.Select(g => g.Key).Join(", ")} が重複しています。");
        }

        // 予約語
        if (NodeOptions.Any(opt => opt.AttributeName == SchemaParseContext.ATTR_NODE_TYPE)) {
            throw new InvalidOperationException($"{SchemaParseContext.ATTR_NODE_TYPE} という名前のオプション属性は定義できません。");
        }
    }

    /// <summary>
    /// 特定のモデルで使用可能なオプショナル属性を列挙します。
    /// </summary>
    public IEnumerable<NodeOption> GetAvailableOptionsFor(IModel model) {
        return NodeOptions.Where(opt => opt.IsAvailableModelMembers == null
                                     || opt.IsAvailableModelMembers(model));
    }
}
