using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Year : SchalarMemberType {
        public override string GetUiDisplayName() => "年";
        public override string GetHelpText() => $"年。西暦で登録されます。";

        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";

        protected override string ComponentName => "Input.Num";
        private protected override IEnumerable<string> RenderAttributes(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            yield return $"className=\"w-16\"";
            yield return $"placeholder=\"0000\"";
        }

        public override string DataTableColumnDefHelperName => "year";
        public override Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 年 */
                const year: {{returnType}} = (header, getValue, setValue, opt) => ({
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
                      const { year } = Util.tryParseAsYearOrEmpty(value)
                      setValue(row, year, rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const { year } = Util.tryParseAsYearOrEmpty(value)
                      setValue(row, year, rowIndex)
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
