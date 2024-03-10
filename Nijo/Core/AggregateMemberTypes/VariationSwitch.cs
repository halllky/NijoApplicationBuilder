using Nijo.Util.CodeGenerating;
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

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            var options = _variationGroup
                .VariationAggregates
                .Select(kv => $"{{ key: '{kv.Key}', text: '{kv.Value.RelationName}' }}");
            var props = new Dictionary<string, string> {
                { "options", $"[{options.Join(", ")}]" },
                { "keySelector", "x => x.key" },
                { "textSelector", "x => x.text" },
            };

            // DataTable内ならばラジオボタンではなくコンボボックス
            if (e.Type == GetReactComponentArgs.E_Type.InDataGrid)
                props.Add("combo", string.Empty);

            return new ReactInputComponent {
                Name = "Input.Selection",
                Props = props,
                GridCellFormatStatement = (value, formatted) => {
                    var keyValues = _variationGroup
                        .VariationAggregates
                        .Select(kv => new { kv.Key, kv.Value.RelationName });
                    return $$"""
                        let {{formatted}} = ''
                        {{keyValues.SelectTextTemplate(kv => $$"""
                        if ({{value}} === '{{kv.Key}}') {{formatted}} = '{{kv.RelationName}}'
                        """)}}
                        """;
                },
            };
        }
    }
}
