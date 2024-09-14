using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDay : SchalarMemberType {

        public override void GenerateCode(CodeRenderingContext context) {
            // クラス定義
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(Parts.Utility.RuntimeDateClass.RenderDeclaring());
            });

            // JavaScriptとC#の間の変換
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(Parts.Utility.RuntimeDateClass.GetCustomJsonConverter());

            // C#とDBの間の変換
            var dbContext = context.UseSummarizedFile<Parts.WebServer.DbContextClass>();
            dbContext.AddOnModelCreatingPropConverter(Parts.Utility.RuntimeDateClass.CLASS_NAME, Parts.Utility.RuntimeDateClass.EFCoreConverterClassFullName);
        }

        public override string GetCSharpTypeName() => Parts.Utility.RuntimeDateClass.CLASS_NAME;
        public override string GetTypeScriptTypeName() => "string";

        protected override string ComponentName => "Input.Date";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-28\"";
        }

        public override IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
                SetValueToRow = (value, parsed) => {
                    return $$"""
                        const { result: {{parsed}} } = Util.tryParseAsDateOrEmpty({{value}})
                        """;
                },
            };
        }

        public override string DataTableColumnDefHelperName => "date";
        public override string RenderDataTableColumnDefHelper() {
            return $$"""
                /** 年月日 */
                date: {{Parts.WebClient.DataTable.CellType.HELPER_MEHOTD_TYPE}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => {
                  this._columns.push({
                    ...opt,
                    id: opt?.id ?? `${opt?.headerGroupName}::${header}`,
                    render: row => <PlainCell>{getValue(row)}</PlainCell>,
                    onClipboardCopy: row => getValue(row) ?? '',
                    editSetting: opt?.readOnly === true ? undefined : {
                      type: 'text',
                      readOnly: typeof opt?.readOnly === 'function'
                        ? opt.readOnly
                        : undefined,
                      onStartEditing: row => getValue(row),
                      onEndEditing: (row, value) => {
                        const { result } = Util.tryParseAsDateOrEmpty(value)
                        setValue(row, result)
                      },
                      onClipboardPaste: (row, value) => {
                        const { result } = Util.tryParseAsDateOrEmpty(value)
                        setValue(row, result)
                      },
                    },
                  })
                  return this
                }
                """;
        }
    }
}
