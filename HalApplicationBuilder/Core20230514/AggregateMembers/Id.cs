using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514.AggregateMembers {
    public class Id : IAggregateMember {
        public PropertyDefinition ToPropertyDefinition() => new PropertyDefinition {
            CSharpTypeName = "string",
            
        };
    }
}
