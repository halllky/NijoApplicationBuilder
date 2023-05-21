using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    public interface IAggregateMember {
        PropertyDefinition ToPropertyDefinition();
    }

    public abstract class CategorizeMember : IAggregateMember {
    }

    public interface ISchalarMember : IAggregateMember {
        object? Min { get; }
        object? Max { get; }
    }
    public interface ISchalarMember<T> : ISchalarMember {
        new T? Min { get; }
        new T? Max { get; }
    }
}
