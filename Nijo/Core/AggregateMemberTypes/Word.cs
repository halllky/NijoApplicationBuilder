using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class Word : CategorizeType {
        public override SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public override string GetCSharpTypeName() => "string";
        public override string GetTypeScriptTypeName() => "string";

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent { Name = "Input.Word" };
        }
    }
}
