using Newtonsoft.Json.Linq;
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
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new ComboboxColumnSetting {
                OptionItemTypeName = $"{{ key: 'T' | 'F', text: string }}",
                Options = $"[{{ key: 'T' as const, text: '✓' }}, {{ key: 'F' as const, text: '' }}]",
                EmitValueSelector = $"opt => opt",
                MatchingKeySelectorFromEmitValue = $"opt => opt.key",
                MatchingKeySelectorFromOption = $"opt => opt.key",
                TextSelector = $"opt => opt.text",

                GetDisplayText = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? '✓' : ''
                    """,
                SetValueToRow = (value, formatted) => $$"""
                    const {{formatted}} = {{value}}?.key === 'T'
                    """,
            };
        }
    }
}
