using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514.AggregateMembers {
    public class Word : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public override string GetCSharpTypeName() => "string";
        public override IEnumerable<string> UserInterface(IGuiForm ui) => ui.TextBox();
    }
}
