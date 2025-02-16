using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Integer : SchalarMemberType {
        public override string GetUiDisplayName() => "整数";
        public override string GetHelpText() => $"整数。";

        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";

        public override string UiConstraintType => "NumberMemberConstraint";

        protected override string ComponentName => "Input.Num";
        private protected override IEnumerable<string> RenderAttributes(AggregateMember.ValueMember vm, FormUIRenderingContext ctx) {
            yield return $"className=\"w-28\"";
        }

        public override string DataTableColumnDefHelperName => "integer";
        public override Parts.WebClient.DataTable.CellType.Helper RenderDataTableColumnDefHelper(CodeRenderingContext ctx) {
            var returnType = $"{Parts.WebClient.DataTable.CellType.RETURNS_ONE_COLUMN}<TRow, {GetTypeScriptTypeName()} | undefined>";
            var body = $$"""
                /** 整数 */
                const integer: {{returnType}} = (header, getValue, setValue, opt) => ({
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
                      const { num } = Util.tryParseAsNumberOrEmpty(value)
                      setValue(row, num, rowIndex)
                    },
                    onClipboardPaste: (row, value, rowIndex) => {
                      const { num } = Util.tryParseAsNumberOrEmpty(value)
                      setValue(row, num, rowIndex)
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
