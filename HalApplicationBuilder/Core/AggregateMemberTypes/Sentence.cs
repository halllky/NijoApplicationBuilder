using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    public class Sentence : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public string GetCSharpTypeName() => "string";
        public string GetTypeScriptTypeName() => "string";
        public string RenderUI(IGuiFormRenderer ui) => ui.TextBox(multiline: true);
        public string GetGridCellEditorName() => "Input.Description";
        public IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
        public virtual string? GetGridCellFormatter() => null;
    }
}
