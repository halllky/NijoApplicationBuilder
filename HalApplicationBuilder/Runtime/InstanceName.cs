using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.Runtime {

    internal class InstanceName : ValueObject {
        internal static InstanceName FromSearchResult(IEnumerable<object?> searchResultMemberValues, Core.Aggregate aggregate) {
            var strValues = searchResultMemberValues
                .Where(x => x != null)
                .Select(x => x!.ToString());
            var value = string.Join(" ", strValues);
            return new InstanceName { Value = value };
        }
        internal static InstanceName FromDbEntity(object dbInstance, Core.Aggregate aggregate) {
            // TODO
            return new InstanceName { Value = "INSTANCE NAME" };
        }

        private InstanceName() { }

        internal required string Value { get; init; }

        protected override IEnumerable<object> ValueObjectIdentifiers() {
            yield return Value;
        }
    }
}
