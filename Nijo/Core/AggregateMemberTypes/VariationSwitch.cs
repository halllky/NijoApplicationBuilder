using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class VariationSwitch : CategorizeType {
        internal VariationSwitch(VariationGroup<Aggregate> variationGroup) {
            _variationGroup = variationGroup;
        }
        private readonly VariationGroup<Aggregate> _variationGroup;

        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override string RenderUI(IGuiFormRenderer ui) => string.Empty;
        public override string GetGridCellEditorName() => "Input.SelectionEmitsKey";

        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() {
            var options = _variationGroup
                .VariationAggregates
                .Select(kv => $"{{ key: '{kv.Key}', text: '{kv.Value.RelationName}' }}");
            return new Dictionary<string, string> {
                { "options", $"[{options.Join(", ")}]" },
                { "keySelector", "x => x.key" },
                { "textSelector", "x => x.text" },
            };
        }

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            var options = _variationGroup
                .VariationAggregates
                .Select(kv => $"{{ key: '{kv.Key}', text: '{kv.Value.RelationName}' }}");
            return new ReactInputComponent {
                Name = "Input.SelectionEmitsKey",
                Props = new Dictionary<string, string> {
                    { "options", $"[{options.Join(", ")}]" },
                    { "keySelector", "x => x.key" },
                    { "textSelector", "x => x.text" },
                },
            };
        }
    }
}
