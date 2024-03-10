using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDayTime : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Range;
        public string GetCSharpTypeName() => "DateTime";
        public string GetTypeScriptTypeName() => "string";

        public ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = "Input.Date",
                GridCellFormatStatement = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} == undefined
                      ? ''
                      : dayjs({{value}}).format('YYYY-MM-DD HH:mm:ss')
                    """,
            };
        }
    }
}
