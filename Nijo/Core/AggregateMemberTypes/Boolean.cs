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

        public ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.CheckBox",
                GridCellFormatStatement = (value, formatted) => $$"""
                    const {{formatted}} = ({{value}} === undefined ? '' : ({{value}} ? '○' : '-'))
                    """,
            };
        }
    }
}
