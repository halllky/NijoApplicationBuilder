using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDay : SchalarMemberType {
        public override string GetUiDisplayName() => "日付";
        public override string GetHelpText() => $"年月日。";

        public override void GenerateCode(CodeRenderingContext context) {
            // クラス定義
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(Parts.Utility.RuntimeDateClass.RenderDeclaring());
            });

            // JavaScriptとC#の間の変換
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(Parts.Utility.RuntimeDateClass.GetCustomJsonConverter());

            // C#とDBの間の変換
            context.UseSummarizedFile<Parts.WebServer.DbContextClass>()
                .AddOnModelCreatingPropConverter(Parts.Utility.RuntimeDateClass.CLASS_NAME, "GetYearMonthDayEFCoreValueConverter");
            context.UseSummarizedFile<Parts.Configure>().AddMethod($$"""
                /// <summary>
                /// <see cref="{{Parts.Utility.RuntimeDateClass.CLASS_NAME}}"/> クラスのプロパティがDBとC#の間で変換されるときの処理を定義するクラスを返します。
                /// </summary>
                public virtual Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter GetYearMonthDayEFCoreValueConverter() {
                    return new {{Parts.Utility.RuntimeDateClass.EFCoreConverterClassFullName}}();
                }
                """);
        }

        public override string GetCSharpTypeName() => Parts.Utility.RuntimeDateClass.CLASS_NAME;
        public override string GetTypeScriptTypeName() => "string";

        protected override string ComponentName => "Input.Date";
        private protected override IEnumerable<string> RenderAttributes(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            yield return $"className=\"w-28\"";
        }

        public override string DataTableColumnDefHelperName => "date";
        public override Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 年月日 */
                const date: {{returnType}} = (header, getValue, setValue, opt) => ({
                  ...opt,
                  id: `${opt?.headerGroupName}::${header}`,
                  header,
                  render: row => <PlainCell>{getValue(row)}</PlainCell>,
                  onClipboardCopy: row => getValue(row) ?? '',
                  editSetting: opt?.readOnly === true ? undefined : {
                    type: 'text',
                    readOnly: typeof opt?.readOnly === 'function'
                      ? opt.readOnly
                      : undefined,
                    onStartEditing: row => getValue(row),
                    onEndEditing: (row, value, rowIndex) => {
                      const { result } = Util.tryParseAsDateOrEmpty(value)
                      setValue(row, result, rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const { result } = Util.tryParseAsDateOrEmpty(value)
                      setValue(row, result, rowIndex)
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
