using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDay : SchalarMemberType {
        public override string GetCSharpTypeName() => "DateTime";
        public override string GetTypeScriptTypeName() => "string";

        public override ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.Date",
                Props = { ["className"] = "\"w-28\"" },
            };
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
    }
}
