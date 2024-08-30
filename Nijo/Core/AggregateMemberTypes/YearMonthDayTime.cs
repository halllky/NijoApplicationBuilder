using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDayTime : SchalarMemberType {
        public override string GetCSharpTypeName() => "DateTime";
        public override string GetTypeScriptTypeName() => "string";

        public override ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.DateTime",
            };
        }

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
    }
}
