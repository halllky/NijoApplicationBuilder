using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDayTime : SchalarMemberType {
        public override string GetUiDisplayName() => "日付時刻";
        public override string GetHelpText() => $"日付時刻。";

        public override string GetCSharpTypeName() => "DateTime";
        public override string GetTypeScriptTypeName() => "string";

        protected override string ComponentName => "Input.DateTime";
        private protected override IEnumerable<string> RenderAttributes(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            yield return $"className=\"w-48\"";
        }

        public override string DataTableColumnDefHelperName => "datetime";
        public override Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 日付時刻 */
                const datetime: {{returnType}} = (header, getValue, setValue, opt) => ({
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
                      const { result } = Util.tryParseAsDateTimeOrEmpty(value)
                      setValue(row, result, rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const { result } = Util.tryParseAsDateTimeOrEmpty(value)
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
