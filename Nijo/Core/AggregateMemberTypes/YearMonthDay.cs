using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDay : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Range;
        public string GetCSharpTypeName() => "DateTime";
        public string GetTypeScriptTypeName() => "string";

        public ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.Date",
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
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
