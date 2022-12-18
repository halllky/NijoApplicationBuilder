using System;
namespace haldoc.Schema.Relation {
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class VariationAttribute : Attribute {

        public string Key { get; }
        public Type Type { get; }

        public VariationAttribute(string key, Type type) {
            Key = key;
            Type = type;
        }
    }
}
