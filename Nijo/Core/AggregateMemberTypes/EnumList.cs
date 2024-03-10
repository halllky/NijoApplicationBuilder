using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class EnumList : CategorizeType {
        public EnumList(EnumDefinition definition) {
            Definition = definition;
        }
        public EnumDefinition Definition { get; }

        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => Definition.Name;
        public override string GetTypeScriptTypeName() {
            return Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ");
        }

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = e.Type == GetReactComponentArgs.E_Type.InDetailView
                    ? "Input.Selection"
                    : "Input.ComboBox",
                Props = new Dictionary<string, string> {
                    { "options", $"[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]" },
                    { "keySelector", "item => item" },
                    { "textSelector", "item => item" },
                },
            };
        }
    }
}
