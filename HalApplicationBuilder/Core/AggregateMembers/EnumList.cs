using HalApplicationBuilder.DotnetEx;
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
        public override string GetTypeScriptTypeName() {
            return Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ");
        }
        public override string RenderUI(IGuiFormRenderer ui) {
            var options = Definition.Items.ToDictionary(
                x => x.PhysicalName,
                x => x.DisplayName ?? x.PhysicalName);
            return ui.Selection(options);
        }
    }
}
