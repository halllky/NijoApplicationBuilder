using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDay : SchalarType<DateTime> {
        public override string GetCSharpTypeName() => "DateTime";
        public override string GetTypeScriptTypeName() => "string";

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = "Input.Date",
                GridCellFormatStatement = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} == undefined
                      ? ''
                      : dayjs({{value}}).format('YYYY-MM-DD')
                    """,
            };
        }
    }
}
