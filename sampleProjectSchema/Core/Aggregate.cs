using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace haldoc.Core {
    public class Aggregate {
        public Aggregate(Type type) {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.GetCustomAttribute<Schema.AggregateRootAttribute>() != null)
                throw new InvalidOperationException($"型 {type.Name} に {nameof(Schema.AggregateRootAttribute)} がついていない");
            _type = type;
        }

        private readonly Type _type;
    }
}
