using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonth : SchalarType<DateTime> {
        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = "Input.YearMonth",
                GridCellValueFormatter = value => $$"""
                    {{value}} == undefined
                      ? ''
                      : `${(Math.floor(value / 100)).toString().padStart(4, '0')}-${(value % 100).toString().padStart(2, '0')}`
                    """,
            };
        }
    }
}
