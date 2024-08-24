using Nijo.Models.ReadModel2Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class Sentence : StringMemberType {
        public override ReactInputComponent GetReactComponent() {
            return new ReactInputComponent { Name = "Input.Description" };
        }
    }
}
