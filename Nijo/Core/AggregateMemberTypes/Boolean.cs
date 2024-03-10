using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Boolean : IAggregateMemberType {
        public string GetCSharpTypeName() => "bool";
        public string GetTypeScriptTypeName() => "boolean";
        public SearchBehavior SearchBehavior => SearchBehavior.Strict;

        public ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = e.Type == GetReactComponentArgs.E_Type.InDataGrid
                    ? "Input.BooleanComboBox"
                    : "Input.CheckBox",
                GridCellFormatStatement = (value, formatted) => $$"""
                    const {{formatted}} = ({{value}} === undefined ? '' : ({{value}} ? '○' : '-'))
                    """,
            };
        }
    }
}
