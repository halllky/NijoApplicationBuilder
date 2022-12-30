using System;
using System.Collections.Generic;

namespace HalApplicationBuilder {
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AggregateAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class InstanceNameAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class VariationAttribute : Attribute {
        public VariationAttribute(int key, Type type) {
            Key = key;
            Type = type;
        }
        public int Key { get; }
        public Type Type { get; }
    }

    public sealed class Child<T> where T : class {
    }

    public sealed class Children<T> : List<T> {
    }
}
