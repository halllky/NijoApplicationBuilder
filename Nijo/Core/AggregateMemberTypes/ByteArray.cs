using Nijo.Parts.WebClient.DataTable;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class ByteArray : IAggregateMemberType {

        public string GetUiDisplayName() {
            return "byte[]";
        }
        public string GetHelpText() {
            return $$"""
                バイト配列。ハッシュ化されたパスワードの保存など限られた場合にのみ登場します。
                この型の項目を画面に表示したり検索条件に指定することはできないため、非表示項目として定義される必要があります。
                """;
        }

        public string GetCSharpTypeName() {
            return "byte[]";
        }
        public string GetTypeScriptTypeName() {
            return "string";
        }


        #region データテーブル列定義
        public string DataTableColumnDefHelperName => "byteArray";
        public CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            return new() {
                FunctionName = DataTableColumnDefHelperName,
                Body = $$"""
                    const {{DataTableColumnDefHelperName}}: {{CellType.RETURNS_ONE_COLUMN}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = () => {
                      throw new Error('Byte配列の列定義はサポートされていません。')
                    }
                    """,
            };
        }
        #endregion データテーブル列定義

        #region 検索条件
        public string GetSearchConditionCSharpType() {
            return "byte[]";
        }
        public string GetSearchConditionTypeScriptType() {
            return "string";
        }
        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            return $$"""
                /* Byte配列 '{{member.DisplayName}}' に対する検索条件の指定はサポートされていません。 */
                """;
        }
        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            return $$"""
                /* Byte配列 '{{vm.DisplayName}}' に対する検索条件の指定はサポートされていません。 */
                """;
        }
        #endregion 検索条件

        string IAggregateMemberType.RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            return $$"""
                /* Byte配列 '{{vm.DisplayName}}' の画面への表示はサポートされていません。 */
                """;
        }
    }
}
