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

        public override ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.Num",
                Props = { ["className"] = "\"w-16\"" },
            };
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
    }
}
