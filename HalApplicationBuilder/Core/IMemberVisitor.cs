using System;
using HalApplicationBuilder.Core.Members;

namespace HalApplicationBuilder.Core {
    internal interface IMemberVisitor {
        void Visit(SchalarValue member);
        void Visit(Child member);
        void Visit(Variation member);
        void Visit(Children member);
        void Visit(Reference member);
    }
}
