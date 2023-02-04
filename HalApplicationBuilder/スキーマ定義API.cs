using System;
using System.Collections.Generic;

namespace HalApplicationBuilder {
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AggregateAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class InstanceNameAttribute : Attribute {
        public int? Order { get; set; }
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

    public sealed class RefTo<T> where T : class {
    }
    /// <summary>
    /// <see cref="RefTo{T}"/> が示す型と対応する集約が複数ある場合、
    /// どの集約に対する参照なのかが特定できないので、その解決策
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class AggregateIdAttribute : Attribute {
        public AggregateIdAttribute(string value) {
            Value = value;
        }
        public string Value { get; }
    }
    /// <summary>
    /// <see cref="RefTo{T}"/> が示す型と対応する集約が複数ある場合、
    /// どの集約に対する参照なのかが特定できないので、その解決策
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RefTargetIdAttribute : Attribute {
        public RefTargetIdAttribute(string value) {
            Value = value;
        }
        public string Value { get; }
    }
}
