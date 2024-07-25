using Nijo.Models.ReadModel2Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class Uuid : StringMemberType {
        protected override E_SearchBehavior SearchBehavior => E_SearchBehavior.Strict;
    }
}
