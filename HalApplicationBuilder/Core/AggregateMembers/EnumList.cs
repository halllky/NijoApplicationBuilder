using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMembers {
    public class EnumList : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => "int?";
        public override string GetTypeScriptTypeName() => "number";
        public override IEnumerable<string> UserInterface(IGuiForm ui) => ui.Selection();
    }
}
