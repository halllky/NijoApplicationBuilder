using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class VariationSwitch : IAggregateMemberType {
        internal VariationSwitch(VariationGroup<Aggregate> variationGroup) {
            _variationGroup = variationGroup;
        }
        private readonly VariationGroup<Aggregate> _variationGroup;

        public SearchBehavior SearchBehavior => SearchBehavior.Strict;

        private string CsEnumTypeName => _variationGroup.CsEnumType;
        public string GetCSharpTypeName() => CsEnumTypeName;

        public string GetTypeScriptTypeName() {
            return _variationGroup
                .VariationAggregates
                .Select(kv => $"'{kv.Value.RelationName}'")
                .Join(" | ");
        }

        public string GetSearchConditionCSharpType() {
            return SearchConditionEnum;
        }
        public string GetSearchConditionTypeScriptType() {
            return $"{{ {_variationGroup.VariationAggregates.Values.Select(edge => $"{edge.Terminal.Item.PhysicalName}?: boolean").Join(", ")} }}";
        }

        private string SearchConditionEnum => $"{_variationGroup.GroupName}SearchCondition";
        void IAggregateMemberType.GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.Enums.Add($$"""
                public class {{SearchConditionEnum}} {
                {{_variationGroup.VariationAggregates.Values.SelectTextTemplate(edge => $$"""
                    public bool {{edge.Terminal.Item.PhysicalName}} { get; set; }
                """)}}
                }
                """);
        }

        public ReactInputComponent GetReactComponent() {
            var props = new Dictionary<string, string> {
                { "options", $"[{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}' as const").Join(", ")}]" },
                { "textSelector", "item => item" },
            };

            return new ReactInputComponent {
                Name = "Input.Selection",
                Props = props,
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new ComboboxColumnSetting {
                OptionItemTypeName = _variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}'").Join(" | "),
                Options = $"[{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}' as const").Join(", ")}]",
                EmitValueSelector = $"opt => opt",
                MatchingKeySelectorFromEmitValue = $"value => value",
                MatchingKeySelectorFromOption = $"opt => opt",
                TextSelector = $"opt => opt",
                OnClipboardCopy = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ?? ''
                    """,
                OnClipboardPaste = (value, formatted) => $$"""
                    let {{formatted}}: {{_variationGroup.VariationAggregates.Select(kv => $"'{kv.Value.RelationName}'").Join(" | ")}} | undefined
                    {{_variationGroup.VariationAggregates.SelectTextTemplate((kv, i) => $$"""
                    {{(i == 0 ? "if" : "} else if")}} ({{value}} === '{{kv.Value.RelationName}}') {
                      {{formatted}} = '{{kv.Value.RelationName}}'
                    """)}}
                    } else {
                      {{formatted}} = undefined
                    }
                    """,
            };
        }
    }
}
