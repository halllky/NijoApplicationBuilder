using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class Id : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public string GetCSharpTypeName() => "string";
        public string GetTypeScriptTypeName() => "string";

        public ReactInputComponent GetReactComponent() {
            return new ReactInputComponent { Name = "Input.Word" };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
            };
        }
    }
}
