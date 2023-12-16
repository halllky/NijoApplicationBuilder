using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Boolean : CategorizeType {
        public override string GetCSharpTypeName() => "bool";
        public override string GetTypeScriptTypeName() => "boolean";
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string RenderUI(IGuiFormRenderer ui) => ui.Toggle();
        public override string GetGridCellEditorName() => "Input.BooleanComboBox";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
        public override string GetGridCellValueFormatter() => "({ value }) => (value === undefined ? '' : (value ? '○' : '-'))";
    }
}
