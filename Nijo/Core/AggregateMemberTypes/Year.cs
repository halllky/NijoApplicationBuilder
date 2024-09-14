using Nijo.Parts.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Year : SchalarMemberType {
        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";

        protected override string ComponentName => "Input.Num";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-16\"";
            yield return $"placeholder=\"0000\"";
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
                        const { year: {{parsed}} } = Util.tryParseAsYearOrEmpty({{value}})
                        """;
                },
            };
        }

        public override string DataTableColumnDefHelperName => "year";
        public override string RenderDataTableColumnDefHelper() {
            return $$"""
                /** 年 */
                year: {{Parts.WebClient.DataTable.CellType.HELPER_MEHOTD_TYPE}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => {
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
                        const { year } = Util.tryParseAsYearOrEmpty(value)
                        setValue(row, year)
                      },
                      onClipboardPaste: (row, value) => {
                        const { year } = Util.tryParseAsYearOrEmpty(value)
                        setValue(row, year)
                      },
                    },
                  })
                  return this
                }
                """;
        }
    }
}
