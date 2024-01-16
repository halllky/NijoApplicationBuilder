using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class VariationSwitch : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override string RenderUI(IGuiFormRenderer ui) => string.Empty;
        public override string GetGridCellEditorName() => "Input.SelectionEmitsKey";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
    }
}