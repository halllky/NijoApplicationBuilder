using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDayTime : SchalarMemberType {
        public override string GetCSharpTypeName() => "DateTime";
        public override string GetTypeScriptTypeName() => "string";

        protected override string ComponentName => "Input.DateTime";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-48\"";
        }

        public override IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
                SetValueToRow = (value, parsed) => {
                    return $$"""
                        const { result: {{parsed}} } = Util.tryParseAsDateTimeOrEmpty({{value}})
                        """;
                },
            };
        }

        public override string DataTableColumnDefHelperName => "datetime";
        public override string RenderDataTableColumnDefHelper() {
            return $$"""
                /** 日付時刻 */
                datetime: {{Parts.WebClient.DataTable.CellType.HELPER_MEHOTD_TYPE}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => {
                  this._columns.push({
                    ...opt,
                    id: opt?.id ?? `${opt?.headerGroupName}::${header}`,
                    header,
                    render: row => <PlainCell>{getValue(row)}</PlainCell>,
                    onClipboardCopy: row => getValue(row) ?? '',
                    editSetting: opt?.readOnly === true ? undefined : {
                      type: 'text',
                      readOnly: typeof opt?.readOnly === 'function'
                        ? opt.readOnly
                        : undefined,
                      onStartEditing: row => getValue(row),
                      onEndEditing: (row, value) => {
                        const { result } = Util.tryParseAsDateTimeOrEmpty(value)
                        setValue(row, result)
                      },
                      onClipboardPaste: (row, value) => {
                        const { result } = Util.tryParseAsDateTimeOrEmpty(value)
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
