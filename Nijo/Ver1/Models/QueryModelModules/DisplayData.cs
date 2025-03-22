using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.QueryModelModules {
    /// <summary>
    /// ReadModelの画面表示用データ
    /// </summary>
    internal class DisplayData {

        internal DisplayData(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;


        /// <summary>C#クラス名</summary>
        internal string CsClassName => $"{_aggregate.PhysicalName}DisplayData";
        /// <summary>TypeScript型名</summary>
        internal string TsTypeName => $"{_aggregate.PhysicalName}DisplayData";

        /// <summary>値が格納されるプロパティの名前（C#）</summary>
        internal const string VALUES_CS = "Values";
        /// <summary>値が格納されるプロパティの名前（TypeScript）</summary>
        internal const string VALUES_TS = "values";
        /// <summary>値クラス名</summary>
        internal string ValueCsClassName => $"{CsClassName}Values";

        /// <summary>メッセージ用構造体 C#クラス名</summary>
        internal string MessageDataCsClassName => $"{CsClassName}Messages";

        /// <summary>読み取り専用か否かが格納されるプロパティの名前（C#）</summary>
        internal const string READONLY_CS = "ReadOnly";
        /// <summary>読み取り専用か否かが格納されるプロパティの名前（TypeScript）</summary>
        internal const string READONLY_TS = "readOnly";
        /// <summary>全項目が読み取り専用か否か（C#）</summary>
        internal const string ALL_READONLY_CS = "AllReadOnly";
        /// <summary>全項目が読み取り専用か否か（TypeScript）</summary>
        internal const string ALL_READONLY_TS = "allReadOnly";
        /// <summary>メッセージ用構造体 C#クラス名</summary>
        internal string ReadOnlyDataCsClassName => $"{CsClassName}ReadOnly";

        /// <summary>
        /// 通常、保存時に追加・更新・削除のどの処理となるかは
        /// <see cref="EXISTS_IN_DB_TS"/>, <see cref="WILL_BE_CHANGED_TS"/>, <see cref="WILL_BE_DELETED_TS"/>
        /// から計算されるが、強制的に追加または更新または削除いずれかの処理を走らせたい場合に指定されるプロパティ
        /// </summary>
        internal const string ADD_MOD_DEL_CS = "AddModDel";
        /// <inheritdoc cref="ADD_MOD_DEL_CS"/>
        internal const string ADD_MOD_DEL_TS = "addModDel";

        /// <summary>このデータがDBに保存済みかどうか（C#）。つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue</summary>
        internal const string EXISTS_IN_DB_CS = "ExistsInDatabase";
        /// <summary>このデータがDBに保存済みかどうか（TypeScript）。つまり新規作成のときはfalse, 閲覧・更新・削除のときはtrue</summary>
        internal const string EXISTS_IN_DB_TS = "existsInDatabase";

        /// <summary>画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる（C#）</summary>
        internal const string WILL_BE_CHANGED_CS = "WillBeChanged";
        /// <summary>画面上で何らかの変更が加えられてから、保存処理の実行でその変更が確定するまでの間、trueになる（TypeScript）</summary>
        internal const string WILL_BE_CHANGED_TS = "willBeChanged";

        /// <summary>画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる（C#）</summary>
        internal const string WILL_BE_DELETED_CS = "WillBeDeleted";
        /// <summary>画面上で削除が指示されてから、保存処理の実行でその削除が確定するまでの間、trueになる（TypeScript）</summary>
        internal const string WILL_BE_DELETED_TS = "willBeDeleted";

        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（C#側）</summary>
        internal const string VERSION_CS = "Version";
        /// <summary>楽観排他制御用のバージョニング情報をもつプロパティの名前（TypeScript側）</summary>
        internal const string VERSION_TS = "version";

        /// <summary>追加・更新・削除のいずれかの区分を返すメソッドの名前</summary>
        internal const string GET_SAVE_TYPE = "GetSaveType";


        /// <summary>
        /// valuesの中に宣言されるメンバーを列挙する。
        /// </summary>
        private IEnumerable<IAggregateMember> GetOwnMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ValueMember) {
                    yield return member;

                } else if (member is RefToMember) {
                    yield return member;

                }
            }
        }

        /// <summary>
        /// 子要素を列挙する。
        /// </summary>
        private IEnumerable<IRelationalMember> GetChildrenMembers() {
            foreach (var member in _aggregate.GetMembers()) {
                if (member is ChildAggreagte child) {
                    yield return child;

                } else if (member is ChildrenAggreagte children) {
                    yield return children;

                }
            }
        }


        internal virtual string RenderCSharpDeclaring(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}}の画面表示用データ。
                /// </summary>
                public partial class {{CsClassName}} {
                    // TODO ver.1
                }
                """;
        }

        internal virtual string RenderTypeScriptType(CodeRenderingContext ctx) {
            return $$"""
                /** {{_aggregate.DisplayName}}の画面表示用データ。 */
                export type {{TsTypeName}} = {
                    // TODO ver.1
                }
                """;
        }


        #region UI用の制約定義
        internal string UiConstraintTypeName => $"{_aggregate.PhysicalName}ConstraintType";
        internal string UiConstraingValueName => $"{_aggregate.PhysicalName}Constraints";
        internal string RenderUiConstraintType(CodeRenderingContext ctx) {
            if (_aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /** {{_aggregate.DisplayName}}の各メンバーの制約の型 */
                type {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(_aggregate), "  ")}}
                }
                """;

            static string RenderMembers(AggregateBase aggregate) {
                var displayData = new DisplayData(aggregate);
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{If(m is ValueMember, () => $$"""
                      {{m.PhysicalName}}: Constraints.{{((ValueMember)m).Type.UiConstraintType}}
                    """).Else(() => $$"""
                      {{m.PhysicalName}}: Constraints.{{UiConstraint.E_Type.MemberConstraintBase}}
                    """)}}
                    """)}}
                    }
                    {{displayData.GetChildrenMembers().SelectTextTemplate(desc => $$"""
                    {{desc.PhysicalName}}: {
                      {{WithIndent(RenderMembers(desc.MemberAggregate), "  ")}}
                    }
                    """)}}
                    """;
            }
        }
        internal string RenderUiConstraintValue(CodeRenderingContext ctx) {
            if (_aggregate is not RootAggregate) throw new InvalidOperationException();

            return $$"""
                /** {{_aggregate.DisplayName}}の各メンバーの制約の具体的な値 */
                export const {{UiConstraingValueName}}: {{UiConstraintTypeName}} = {
                  {{WithIndent(RenderMembers(_aggregate), "  ")}}
                }
                """;

            static string RenderMembers(AggregateBase aggregate) {
                var displayData = new DisplayData(aggregate);
                return $$"""
                    {{VALUES_TS}}: {
                    {{displayData.GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{If(m is ValueMember, () => $$"""
                      {{m.PhysicalName}}: {
                    {{If(((ValueMember)m).IsKey || ((ValueMember)m).IsRequired, () => $$"""
                        {{UiConstraint.MEMBER_REQUIRED}}: true,
                    """)}}
                    {{If(((ValueMember)m).CharacterType != null, () => $$"""
                        {{UiConstraint.MEMBER_CHARACTER_TYPE}}: {{((ValueMember)m).CharacterType}},
                    """)}}
                    {{If(((ValueMember)m).MaxLength != null, () => $$"""
                        {{UiConstraint.MEMBER_MAX_LENGTH}}: {{((ValueMember)m).MaxLength}},
                    """)}}
                    {{If(((ValueMember)m).TotalDigit != null, () => $$"""
                        {{UiConstraint.MEMBER_TOTAL_DIGIT}}: {{((ValueMember)m).TotalDigit}},
                    """)}}
                    {{If(((ValueMember)m).DecimalPlace != null, () => $$"""
                        {{UiConstraint.MEMBER_DECIMAL_PLACE}}: {{((ValueMember)m).DecimalPlace}},
                    """)}}
                      },
                    """).ElseIf(m is RefToMember, () => $$"""
                      {{m.PhysicalName}}: {
                    {{If(((RefToMember)m).IsKey || ((RefToMember)m).IsRequired, () => $$"""
                        {{UiConstraint.MEMBER_REQUIRED}}: true,
                    """)}}
                      },
                    """)}}
                    """)}}
                    },
                    {{displayData.GetChildrenMembers().SelectTextTemplate(desc => $$"""
                    {{desc.PhysicalName}}: {
                      {{WithIndent(RenderMembers(desc.MemberAggregate), "  ")}}
                    },
                    """)}}
                    """;
            }
        }
        #endregion UI用の制約定義


        #region TypeScript新規オブジェクト作成関数
        /// <summary>
        /// TypeScriptの新規オブジェクト作成関数の名前
        /// </summary>
        internal string CreateNewObjectFnName => $"createNew{TsTypeName}";
        internal string RenderTypeScriptObjectCreationFunction(CodeRenderingContext ctx) {
            return $$"""
                /** {{_aggregate.DisplayName}}の画面表示用データの新しいインスタンスを作成します。 */
                export const {{CreateNewObjectFnName}} = (): {{TsTypeName}} => ({
                  // TODO ver.1
                })
                """;
        }
        #endregion TypeScript新規オブジェクト作成関数


        /// <summary>
        /// <see cref="SearchResult"/> のインスタンスを <see cref="DisplayData"/> のインスタンスに変換する処理
        /// </summary>
        internal string ConvertSearchResultToDisplayData(CodeRenderingContext ctx) {
            var searchResult = new SearchResult(_aggregate);

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}}の検索結果を画面表示用データに変換します。
                /// </summary>
                protected virtual {{CsClassName}} ToDisplayData({{searchResult.CsClassName}} searchResult) {
                    throw new NotImplementedException(); // TODO ver.1
                }
                """;
        }
    }
}
