using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.WebClient.DataTable;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    /// <summary>
    /// 添付ファイル型。
    /// 実行時の型はアプリケーションテンプレート側のプロジェクトで定義しています。
    /// </summary>
    internal class FileAttachment : IAggregateMemberType {

        // 添付ファイルのメタデータはDBにJSONで保存されるので、そのJSONのプロパティ名
        private const string C_METADATA = "metadata";
        private const string C_WILL_DETACH = "willDetach";
        private const string C_DISPLAY_FILE_NAME = "displayFileName";
        private const string C_HREF = "href";
        private const string C_DOWNLOAD = "download";
        private const string C_OTHER_PROPS = "othreProps";


        public string GetCSharpTypeName() => "FileAttachment";
        public string GetTypeScriptTypeName() => "Input.FileAttachment";

        public string GetUiDisplayName() => "ファイル";
        public string GetHelpText() => $$"""
            添付ファイル型。
            この型のインスタンスは、アップロードする時のみ、そのファイルのバイナリを保持します。
            それ以外の時は、アップロードされたファイルのメタデータのみを保持します。
            """;


        // ファイルの検索はメタデータのJSONに対する部分一致検索とする。
        public string GetSearchConditionCSharpType() => "string";
        public string GetSearchConditionTypeScriptType() => "string";
        public string RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
              ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
              : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
            var whereFullpath = searchQueryObject == E_SearchQueryObject.SearchResult
                ? member.GetFullPathAsSearchResult(E_CsTs.CSharp, out var isArray)
                : member.GetFullPathAsDbEntity(E_CsTs.CSharp, out isArray);

            return $$"""
                if (!string.IsNullOrWhiteSpace({{fullpathNullable}})) {
                    var trimmed = {{fullpathNotNull}}.Trim();
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.SkipLast(1).Join(".")}}.Any(y => y.{{member.MemberName}}.Contains(trimmed)));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{whereFullpath.Join(".")}}.Contains(trimmed));
                """)}}
                }
                """;
        }
        public string RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var attrs = new List<string>();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            attrs.Add($"{{...{ctx.Register}(`{fullpath}`)}}");
            return $$"""
                <Input.Word {{attrs.Join(" ")}}/>
                """;
        }


        public string RenderSingleViewVFormBody(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            var attrs = new List<string>();
            var fullpath = ctx.GetReactHookFormFieldPath(vm.Declared).Join(".");
            attrs.Add($"{{...{ctx.Register}(`{fullpath}`)}}");
            attrs.Add(ctx.RenderReadOnlyStatement(vm.Declared));
            if (vm.Options.UiWidth != null) attrs.Add($"className=\"min-w-[{vm.Options.UiWidth.GetCssValue()}]\"");

            return $$"""
                <Input.FileAttachmentView {{attrs.Join(" ")}}/>
                {{ctx.RenderErrorMessage(vm)}}
                """;
        }


        // DataTableの列にはファイル名を表示する。
        public string DataTableColumnDefHelperName => "file";
        public CellType.Helper RenderDataTableColumnDefHelper() {
            var body = $$"""
                /** 添付ファイル */
                const {{DataTableColumnDefHelperName}}: {{CellType.RETURNS_ONE_COLUMN}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => ({
                  ...opt,
                  id: `${opt?.headerGroupName}::${header}`,
                  header,
                  render: row => {
                    const isReadOnly = typeof opt?.readOnly === 'function'
                      ? (opt.readOnly(row))
                      : (opt?.readOnly ?? false)

                    return (
                      <PlainCell>
                        {getValue(row)?.{{C_METADATA}}?.{{C_DISPLAY_FILE_NAME}}}
                      </PlainCell>
                    )
                  },
                  onClipboardCopy: row => getValue(row)?.{{C_METADATA}}?.{{C_DISPLAY_FILE_NAME}} ?? '',
                })
                """;
            return new() {
                FunctionName = DataTableColumnDefHelperName,
                Body = body,
            };
        }


        // C#とDBとの型変換。DBにはメタデータのJSONを保存する。
        // クラス定義はテンプレートプロジェクトにあるのでそちらを参照。
        void IAggregateMemberType.GenerateCode(CodeRenderingContext context) {
            var dbContext = context.UseSummarizedFile<Parts.WebServer.DbContextClass>();
            dbContext.AddOnModelCreatingPropConverter(GetCSharpTypeName(), "EFCoreFileAttachmentConverter");
        }
    }
}
