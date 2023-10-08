using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    public class Word : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override string RenderUI(IGuiFormRenderer ui) => ui.TextBox();
        public override string GetGridCellEditorName() => "'agTextCellEditor'";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
    }
}
