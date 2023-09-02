using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMembers {
    public class EnumList : CategorizeType {
        public EnumList(EnumDefinition definition) {
            Definition = definition;
        }
        public EnumDefinition Definition { get; }

        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => $"{Definition.Name}?";
        public override string GetTypeScriptTypeName() => "number";
        public override IEnumerable<string> RenderUI(IGuiFormRenderer ui) {
            var options = Definition.Items.ToDictionary(x => x.Value.ToString(), x => x.Name);
            return ui.Selection(options);
        }
    }
}
