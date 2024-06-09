using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonth : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Range;
        public string GetCSharpTypeName() => "int";
        public string GetTypeScriptTypeName() => "number";

        public ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.YearMonth",
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
                GetValueFromRow = (value, formatted) => {
                    return $$"""
                        const {{formatted}} = {{value}}?.toString()
                        """;
                },
                SetValueToRow = (value, parsed) => {
                    return $$"""
                        const { yyyymm: {{parsed}} } = Util.tryParseAsYearMonthOrEmpty({{value}})
                        """;
                },
            };
        }
    }
}
