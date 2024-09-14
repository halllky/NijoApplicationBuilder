using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonth : SchalarMemberType {

        public override void GenerateCode(CodeRenderingContext context) {
            // クラス定義
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(Parts.Utility.RuntimeYearMonthClass.RenderDeclaring());
            });

            // JavaScriptとC#の間の変換
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(Parts.Utility.RuntimeYearMonthClass.GetCustomJsonConverter());

            // C#とDBの間の変換
            var dbContext = context.UseSummarizedFile<Parts.WebServer.DbContextClass>();
            dbContext.AddOnModelCreatingPropConverter(Parts.Utility.RuntimeYearMonthClass.CLASS_NAME, Parts.Utility.RuntimeYearMonthClass.EFCoreConverterClassFullName);
        }

        public override string GetCSharpTypeName() => Parts.Utility.RuntimeYearMonthClass.CLASS_NAME;
        public override string GetTypeScriptTypeName() => "number";

        protected override string ComponentName => "Input.YearMonth";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-20\"";
        }

        public override IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
                GetValueFromRow = (value, formatted) => {
                    return $$"""
                        const {{formatted}} = {{value}}?.toString()
                        """;
                },
                SetValueToRow = (value, parsed) => {
                    return $$"""
                        const { yyyymm: {{parsed}} } = Util.tryParseAsYearMonthOrEmpty({{value}})
                        """;
                },
            };
        }

        public override string DataTableColumnDefHelperName => "yearMonth";
        public override string RenderDataTableColumnDefHelper() {
            return $$"""
                /** 年月 */
                yearMonth: {{Parts.WebClient.DataTable.CellType.HELPER_MEHOTD_TYPE}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => {
                  this._columns.push({
                    ...opt,
                    id: opt?.id ?? `${opt?.headerGroupName}::${header}`,
                    render: row => <PlainCell>{getValue(row)}</PlainCell>,
                    onClipboardCopy: row => getValue(row)?.toString() ?? '',
                    editSetting: opt?.readOnly === true ? undefined : {
                      type: 'text',
                      readOnly: typeof opt?.readOnly === 'function'
                        ? opt.readOnly
                        : undefined,
                      onStartEditing: row => getValue(row)?.toString(),
                      onEndEditing: (row, value) => {
                        const { yyyymm } = Util.tryParseAsYearMonthOrEmpty(value)
                        setValue(row, yyyymm)
                      },
                      onClipboardPaste: (row, value) => {
                        const { yyyymm } = Util.tryParseAsYearMonthOrEmpty(value)
                        setValue(row, yyyymm)
                      },
                    },
                  })
                  return this
                }
                """;
        }
    }
}
