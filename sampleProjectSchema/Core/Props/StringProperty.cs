using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace haldoc.Core.Props {
    public class StringProperty {
        public StringProperty(PropertyInfo propInfo) {
            if (propInfo == null)
                throw new ArgumentNullException(nameof(propInfo));
            if (propInfo.PropertyType != typeof(string))
                throw new InvalidOperationException($"プロパティ {propInfo.Name} は string 型ではない");
            _propInfo = propInfo;
        }
        private readonly PropertyInfo _propInfo;

        public IEnumerable<Schema.EntityPropDef> ToEntityProperty() {
            yield return new Schema.EntityPropDef {
                TypeName = "string",
                ColumnName = _propInfo.Name,
            };
        }
        public object CreateDefaultValue() {
            return "";
        }
    }
}
