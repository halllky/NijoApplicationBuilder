using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Integer : SchalarMemberType {
        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";

        protected override string ComponentName => "Input.Num";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-28\"";
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
                        const { num: {{parsed}} } = Util.tryParseAsNumberOrEmpty({{value}})
                        """;
                },
            };
        }

        public override string DataTableColumnDefHelperName => "integer";
        public override string RenderDataTableColumnDefHelper() {
            return $$"""
                /** 整数 */
                integer: {{Parts.WebClient.DataTable.CellType.HELPER_MEHOTD_TYPE}}<TRow, {{GetTypeScriptTypeName()}} | undefined> = (header, getValue, setValue, opt) => {
                  this._columns.push({
                    ...opt,
                    id: opt?.id ?? `${opt?.headerGroupName}::${header}`,
                    header,
                    render: row => <PlainCell>{getValue(row)}</PlainCell>,
                    onClipboardCopy: row => getValue(row)?.toString() ?? '',
                    editSetting: opt?.readOnly === true ? undefined : {
                      type: 'text',
                      readOnly: typeof opt?.readOnly === 'function'
                        ? opt.readOnly
                        : undefined,
                      onStartEditing: row => getValue(row)?.toString(),
                      onEndEditing: (row, value) => {
                        const { num } = Util.tryParseAsNumberOrEmpty(value)
                        setValue(row, num)
                      },
                      onClipboardPaste: (row, value) => {
                        const { num } = Util.tryParseAsNumberOrEmpty(value)
                        setValue(row, num)
                      },
                    },
                  })
                  return this
                }
                """;
        }
    }
}
