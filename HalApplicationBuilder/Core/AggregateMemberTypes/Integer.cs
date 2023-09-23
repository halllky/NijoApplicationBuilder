using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    internal class Integer : SchalarType<int> {
        public override string GetCSharpTypeName() => "int?";
        public override string GetTypeScriptTypeName() => "number";
        public override string RenderUI(IGuiFormRenderer ui) => ui.TextBox();
    }
}