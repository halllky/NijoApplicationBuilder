using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Numeric : SchalarType<decimal> {
        public override string GetCSharpTypeName() => "decimal";
        public override string GetTypeScriptTypeName() => "number";

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent { Name = "Input.Num" };
        }
    }
}
