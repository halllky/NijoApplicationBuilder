using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class Sentence : StringMemberType {
        protected override bool MultiLine => true;
    }
}
