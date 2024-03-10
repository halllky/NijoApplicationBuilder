using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Year : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Range;
        public string GetCSharpTypeName() => "int";
        public string GetTypeScriptTypeName() => "number";

        public ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent { Name = "Input.Num" };
        }
    }
}
