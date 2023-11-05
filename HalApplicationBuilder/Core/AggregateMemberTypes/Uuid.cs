using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    public class Uuid : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override string RenderUI(IGuiFormRenderer ui) => ui.TextBox();
        public override string GetGridCellEditorName() => "Input.Word";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
    }
}
