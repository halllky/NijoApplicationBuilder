using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.QueryModelModules;

/// <summary>
/// UIコンポーネントの桁数などの制約
/// </summary>
public class UiConstraint {

    /// <summary>
    /// UIコンポーネントの桁数などの制約
    /// </summary>
    public enum E_Type {
        /// <summary>
        /// 種類によらずメンバー毎に定義される制約
        /// </summary>
        MemberConstraintBase,
        /// <summary>
        /// 文字列系の項目でメンバー毎に定義される制約
        /// </summary>
        StringMemberConstraint,
        /// <summary>
        /// 数値系の項目でメンバー毎に定義される制約
        /// </summary>
        NumberMemberConstraint,
    }

    /// <summary>
    /// どの型の制約がくるか分からないときに使う
    /// </summary>
    public const string TYPE_ANY = "AnyMemberConstraints";

    internal const string MEMBER_REQUIRED = "required";
    internal const string MEMBER_MAX_LENGTH = "maxLength";
    internal const string MEMBER_CHARACTER_TYPE = "characterType";
    internal const string MEMBER_TOTAL_DIGIT = "totalDigit";
    internal const string MEMBER_DECIMAL_PLACE = "decimalPlace";

    internal static SourceFile RenderCommonConstraint(CodeRenderingContext ctx) {
        var charTypes = ctx.GetCharacterTypes().ToArray();

        return new SourceFile {
            FileName = "constraints.ts",
            Contents = $$"""
                /** AggregateMemberの制約 */
                export type {{nameof(E_Type.MemberConstraintBase)}} = {
                  /** 必須か否か */
                  {{MEMBER_REQUIRED}}?: boolean
                }

                /** 単語型の制約 */
                export type {{nameof(E_Type.StringMemberConstraint)}} = {{nameof(E_Type.MemberConstraintBase)}} & {
                  /** 最大長。文字数でカウントする */
                  {{MEMBER_MAX_LENGTH}}?: number
                  /** この値がとることのできる文字種。未指定の場合は制約なし */
                  {{MEMBER_CHARACTER_TYPE}}?: {{(charTypes.Length == 0 ? "never" : charTypes.Select(type => $"'{type}'").Join(" | "))}}
                }

                /** 整数型と実数型の制約 */
                export type {{nameof(E_Type.NumberMemberConstraint)}} = {{nameof(E_Type.MemberConstraintBase)}} & {
                  /** 整数部と小数部をあわせた桁数 */
                  {{MEMBER_TOTAL_DIGIT}}?: number
                  /** 小数部桁数 */
                  {{MEMBER_DECIMAL_PLACE}}?: number
                }

                /** いずれかの型の制約 */
                export type {{TYPE_ANY}} = Partial<
                  {{nameof(E_Type.StringMemberConstraint)}}
                  & {{nameof(E_Type.NumberMemberConstraint)}}
                >
                """,
        };
    }
}

/// <summary>
/// 桁数の数字や文字列の最大長の数字などを保持しているオブジェクト
/// </summary>
public interface IUiConstraintValue {
    /// <summary>必須か否か</summary>
    bool IsRequired { get; }
    /// <summary>文字種。半角、半角英数、など</summary>
    string? CharacterType { get; }
    /// <summary>文字列系属性の最大長</summary>
    int? MaxLength { get; }
    /// <summary>数値系属性の整数部桁数 + 小数部桁数</summary>
    int? TotalDigit { get; }
    /// <summary>数値系属性の小数部桁数</summary>
    int? DecimalPlace { get; }
}
