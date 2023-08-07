using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMembers {
    public class Id : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";
        public override IEnumerable<string> UserInterface(IGuiForm ui) => ui.TextBox();
    }
}
