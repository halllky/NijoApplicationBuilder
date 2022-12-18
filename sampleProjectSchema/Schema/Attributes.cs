using System;
using System.Collections.Generic;

namespace haldoc.Schema {
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class AggregateRootAttribute : Attribute {
    }
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class AggregateChildAttribute : Attribute {
    }
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class ChildrenAttribute : Attribute {
    }
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class InstanceNameAttribute : Attribute {
    }
}
