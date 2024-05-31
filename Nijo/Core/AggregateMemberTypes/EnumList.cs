using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class EnumList : IAggregateMemberType {
        public EnumList(EnumDefinition definition) {
            Definition = definition;
        }
        public EnumDefinition Definition { get; }

        public SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public string GetCSharpTypeName() => Definition.Name;
        public string GetTypeScriptTypeName() {
            return Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ");
        }

        public ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            var props = new Dictionary<string, string> {
                { "options", $"[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]" },
                { "textSelector", "item => item" },
            };
            if (e.Type == GetReactComponentArgs.E_Type.InDataGrid) {
                props.Add("combo", "");
            }

            return new ReactInputComponent {
                Name = "Input.Selection",
                Props = props,
            };
        }
    }
}
