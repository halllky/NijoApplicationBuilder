using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// UIコンポーネントの桁数などの制約
    /// </summary>
    internal class UiConstraintTypes : ISummarizedFile {

        internal void Add(DataClassForDisplay displayData) {
            _displayDataList.Add(displayData);
        }
        private readonly List<DataClassForDisplay> _displayDataList = new();

        public void OnEndGenerating(CodeRenderingContext context) {
            if (context.Config.CustomizeAllUi) {

                context.ReactProject.UtilDir(dir => {
                    dir.Generate(new SourceFile {
                        FileName = "constraints.ts",
                        RenderContent = ctx => {
                            return RenderCommonConstraint();
                        },
                    });
                });

                foreach (var disp in _displayDataList) {
                    var aggregateFile = context.CoreLibrary.UseAggregateFile(disp.Aggregate);
                    aggregateFile.TypeScriptFile.Add($$"""
                        {{WithIndent(disp.RenderUiConstraintType(context), "")}}
                        {{WithIndent(disp.RenderUiConstraintValue(context), "")}}
                        """);
                }

            } else {
                context.ReactProject.Types.Add(Render(context));
            }
        }

        private static string RenderCommonConstraint() {
            return $$"""
                /** AggregateMemberの制約 */
                export type MemberConstraintBase = {
                  /** 必須か否か */
                  required?: boolean
                }

                /** 単語型の制約 */
                export type StringMemberConstraint = MemberConstraintBase & {
                  /** 最大長。文字数でカウントする */
                  maxLength?: number
                  /** この値がとることのできる文字種。未指定の場合は制約なし */
                  characterType?: {{Enum.GetValues<Core.E_CharacterType>().Select(type => $"'{type}'").Join(" | ")}}
                }

                /** 整数型と実数型の制約 */
                export type NumberMemberConstraint = MemberConstraintBase & {
                  /** 整数部と小数部をあわせた桁数 */
                  totalDigit?: number
                  /** 小数部桁数 */
                  decimalPlace?: number
                }

                /** いずれかの型の制約 */
                export type AnyMemberConstraints = Partial<
                  StringMemberConstraint
                  & NumberMemberConstraint
                >
                """;
        }

        private string Render(CodeRenderingContext ctx) {
            return $$"""
                {{RenderCommonConstraint()}}

                //#region UI制約
                {{_displayDataList.SelectTextTemplate(disp => $$"""
                {{WithIndent(disp.RenderUiConstraintType(ctx), "")}}
                {{WithIndent(disp.RenderUiConstraintValue(ctx), "")}}

                """)}}
                /** UI制約型一覧 */
                export const ReadModelConstraints = {
                {{_displayDataList.SelectTextTemplate(disp => $$"""
                  '{{disp.TsTypeName}}': {{disp.UiConstraingValueName}},
                """)}}
                }
                //#endregion UI制約
                """;
        }
    }
}
