using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class Sentence : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Ambiguous;
        public string GetCSharpTypeName() => "string";
        public string GetTypeScriptTypeName() => "string";

        public ReactInputComponent GetReactComponent() {
            return new ReactInputComponent { Name = "Input.Description" };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
            };
        }
    }
}
