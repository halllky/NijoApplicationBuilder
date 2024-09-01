using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Numeric : SchalarMemberType {
        public override string GetCSharpTypeName() => "decimal";
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
    }
}
