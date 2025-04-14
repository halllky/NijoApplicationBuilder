using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
