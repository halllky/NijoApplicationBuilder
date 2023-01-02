using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {

    public class UIClass {
        internal UIClass() { }

        public Aggregate Source { get; set; }
        public Func<UIProperty> Parent { get; set; }
        public UIClass GetRoot() {
            var cls = this;
            while (cls.Parent != null) {
                cls = cls.Parent().Owner;
            }
            return cls;
        }

        public string ClassName { get; init; }
        public string RuntimeFullName { get; init; }
        public IReadOnlyList<UIProperty> Properties { get; init; }

        public override string ToString() {
            var path = new List<string>();
            var parent = Parent;
            while (parent != null) {
                path.Insert(0, parent().PropertyName);
                parent = parent().Owner.Parent;
            }
            path.Insert(0, GetRoot().ClassName);
            return $"{nameof(UIClass)}[{string.Join(".", path)}]";
        }
    }

    public class UIProperty {
        internal UIProperty() { }

        public AggregateMemberBase Source { get; set; }
        public UIClass Owner { get; set; }

        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }

        public override string ToString() {
            var path = new List<string>();
            path.Insert(0, PropertyName);

            var parent = Owner.Parent;
            while (parent != null) {
                path.Insert(0, parent().PropertyName);
                parent = parent().Owner.Parent;
            }
            path.Insert(0, Owner.GetRoot().ClassName);
            return $"{nameof(UIProperty)}[{string.Join(".", path)}]";
        }
    }
}
