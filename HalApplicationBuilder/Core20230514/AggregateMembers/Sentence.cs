using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514.AggregateMembers {
    public class Sentence : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public string GetCSharpTypeName() => "string";
        public IEnumerable<string> UserInterface(IGuiForm ui) => ui.TextBox(multiline: true);
    }
}