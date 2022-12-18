using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace haldoc.Core.Props {
    public class ChildrenProperty<T> {
        public ChildrenProperty() {
        }
        public IEnumerable<Schema.EntityColumnDef> ToTableColumn() {
            yield break;
        }
        public object CreateInstanceValue() {
            return new Schema.Children<T>();
        }
    }
}
