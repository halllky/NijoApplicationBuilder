using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMembers {
    internal class TimePoint : SchalarType<DateTime> {
        public override string GetCSharpTypeName() => "DateTime?";
        public override string GetTypeScriptTypeName() => "string";
        public override IEnumerable<string> RenderUI(IGuiFormRenderer ui) => ui.TextBox();
    }
}
