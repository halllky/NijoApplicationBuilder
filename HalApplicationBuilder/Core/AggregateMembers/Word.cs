using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMembers {
    public class Word : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override IEnumerable<string> UserInterface(IGuiForm ui) => ui.TextBox();
    }
}
