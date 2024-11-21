using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonth : SchalarMemberType {
        public override string GetUiDisplayName() => "年月";
        public override string GetHelpText() => $"年月。";

        public override void GenerateCode(CodeRenderingContext context) {
            // クラス定義
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(Parts.Utility.RuntimeYearMonthClass.RenderDeclaring());
            });

            // JavaScriptとC#の間の変換
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(Parts.Utility.RuntimeYearMonthClass.GetCustomJsonConverter(context));

            // C#とDBの間の変換
            context.UseSummarizedFile<Parts.WebServer.DbContextClass>()
                .AddOnModelCreatingPropConverter(Parts.Utility.RuntimeYearMonthClass.CLASS_NAME, "GetYearMonthEFCoreValueConverter");
            context.UseSummarizedFile<Parts.Configure>().AddMethod($$"""
                /// <summary>
                /// <see cref="{{Parts.Utility.RuntimeYearMonthClass.CLASS_NAME}}"/> クラスのプロパティがDBとC#の間で変換されるときの処理を定義するクラスを返します。
                /// </summary>
                public virtual Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter GetYearMonthEFCoreValueConverter() {
                    return new {{Parts.Utility.RuntimeYearMonthClass.EFCoreConverterClassFullName}}();
                }
                """);
        }

        public override string GetCSharpTypeName() => Parts.Utility.RuntimeYearMonthClass.CLASS_NAME;
        public override string GetTypeScriptTypeName() => "number";

        protected override string ComponentName => "Input.YearMonth";
        private protected override IEnumerable<string> RenderAttributes(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            yield return $"className=\"w-20\"";
        }

        public override string DataTableColumnDefHelperName => "yearMonth";
        public override Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 年月 */
                const yearMonth: {{returnType}} = (header, getValue, setValue, opt) => ({
                  ...opt,
                  id: `${opt?.headerGroupName}::${header}`,
                  header,
                  render: row => <PlainCell>{getValue(row)}</PlainCell>,
                  onClipboardCopy: row => getValue(row)?.toString() ?? '',
                  editSetting: opt?.readOnly === true ? undefined : {
                    type: 'text',
                    readOnly: typeof opt?.readOnly === 'function'
                      ? opt.readOnly
                      : undefined,
                    onStartEditing: row => getValue(row)?.toString(),
                    onEndEditing: (row, value, rowIndex) => {
                      const { yyyymm } = Util.tryParseAsYearMonthOrEmpty(value)
                      setValue(row, yyyymm, rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const { yyyymm } = Util.tryParseAsYearMonthOrEmpty(value)
                      setValue(row, yyyymm, rowIndex)
                    },
                  },
                })
                """;
            return new() {
                Body = body,
                FunctionName = DataTableColumnDefHelperName,
                ReturnType = returnType,
            };
        }
    }
}
