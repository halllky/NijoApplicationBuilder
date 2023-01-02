using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {

    public class UIClass {
        public Aggregate Source { get; init; }

        public string ClassName { get; init; }
        public string RuntimeFullName { get; init; }
        public IReadOnlyList<UIProperty> Properties { get; init; }
    }

    public class UIProperty {
        public AggregateMemberBase Source { get; set; }

        public bool Virtual { get; init; }
        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }
    }
}
