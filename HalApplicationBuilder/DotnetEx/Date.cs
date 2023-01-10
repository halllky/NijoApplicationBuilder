using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.DotnetEx {
    public class Date : ValueObject {
        public Date(DateTime? value) {
            Value = value?.Date;
        }

        public DateTime? Value { get; }

        protected override IEnumerable<object> ValueObjectIdentifiers() {
            yield return Value;
        }

        public override string ToString() {
            return Value?.ToShortDateString();
        }
    }
}
