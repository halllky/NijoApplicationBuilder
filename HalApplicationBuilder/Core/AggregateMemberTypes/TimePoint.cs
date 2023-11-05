using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    internal class TimePoint : SchalarType<DateTime> {
        public override string GetCSharpTypeName() => "DateTime?";
        public override string GetTypeScriptTypeName() => "string";
        public override string RenderUI(IGuiFormRenderer ui) => ui.TextBox();
        public override string GetGridCellEditorName() => "Input.Date";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
    }
}
