using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    internal class VariationSwitch : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override string RenderUI(IGuiFormRenderer ui) => string.Empty;
        public override string GetGridCellEditorName() => "'agTextCellEditor'";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
    }
}
