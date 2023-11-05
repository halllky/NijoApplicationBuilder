using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    internal class Boolean : CategorizeType {
        public override string GetCSharpTypeName() => "bool?";
        public override string GetTypeScriptTypeName() => "boolean";
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string RenderUI(IGuiFormRenderer ui) => ui.Toggle();
        public override string GetGridCellEditorName() => "Input.ComboBox";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string> {
            { "options", $"['○' as const, '-' as const]" },
            { "keySelector", "(item: string) => item" },
            { "textSelector", "(item: string) => item" },
        };
    }
}
