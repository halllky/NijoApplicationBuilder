using System;
using System.Collections.Generic;

namespace haldoc.Schema {
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class AggregateRootAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
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
}
