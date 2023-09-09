using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.DotnetEx {

    public abstract class ValueObject {

        protected abstract IEnumerable<object?> ValueObjectIdentifiers();

        public override bool Equals(object? obj) {
            if (obj == null) return false;

            var objType = obj.GetType();
            var thisType = GetType();
            if (objType != thisType
                && !objType.IsSubclassOf(thisType)
                && !thisType.IsSubclassOf(objType)) return false;

            return ValueObjectIdentifiers().SequenceEqual(((ValueObject)obj).ValueObjectIdentifiers());
        }
        public override int GetHashCode() {
            return ValueObjectIdentifiers()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }
        public static bool operator ==(ValueObject? left, ValueObject? right) {
            if (left is null ^ right is null) return false;
            return ReferenceEquals(left, right) || left!.Equals(right);
        }
        public static bool operator !=(ValueObject? left, ValueObject? right) {
            return !(left == right);
        }
    }
}
