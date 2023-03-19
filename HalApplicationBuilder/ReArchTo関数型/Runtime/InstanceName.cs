using System;
using System.Collections.Generic;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime {

    internal class InstanceName : ValueObject {
        internal static InstanceName Create(object dbInstance, Core.Aggregate aggregate) {
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
