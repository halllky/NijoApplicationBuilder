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

        public string GetSearchConditionCSharpType() => BOOL_SEARCH_CONDITION_ENUM;
        public string GetSearchConditionTypeScriptType() => "'指定なし' | 'Trueのみ' | 'Falseのみ'";

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new ComboboxColumnSetting {
                OptionItemTypeName = $"{{ key: 'T' | 'F', text: string }}",
                Options = $"[{{ key: 'T' as const, text: '✓' }}, {{ key: 'F' as const, text: '' }}]",
                EmitValueSelector = $"opt => opt",
                MatchingKeySelectorFromEmitValue = $"opt => opt.key",
                MatchingKeySelectorFromOption = $"opt => opt.key",
                TextSelector = $"opt => opt.text",

                GetValueFromRow = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? { key: 'T' as const, text: '✓' } : { key: 'F' as const, text: '' }
                    """,

                GetDisplayText = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? '✓' : ''
                    """,
                SetValueToRow = (value, formatted) => $$"""
                    const {{formatted}} = {{value}}?.key === 'T'
                    """,

                OnClipboardCopy = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? '✓' : ''
                    """,
                OnClipboardPaste = (value, formatted) => $$"""
                    const normalized = {{value}}.trim().toUpperCase()
                    const {{formatted}} = normalized !== ''
                      && normalized !== 'FALSE'
                      && normalized !== '0'
                    """,
            };
        }

        private const string BOOL_SEARCH_CONDITION_ENUM = "E_BoolSearchCondition";
        void IAggregateMemberType.GenerateCode(Util.CodeGenerating.CodeRenderingContext context) {
            context.CoreLibrary.Enums.Add($$"""
                public enum {{BOOL_SEARCH_CONDITION_ENUM}} {
                    指定なし,
                    Trueのみ,
                    Falseのみ,
                }
                """);
        }
    }
}
