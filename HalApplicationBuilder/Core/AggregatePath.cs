using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    /// <summary>
    /// スキーマ内で集約を一意に識別するための文字列
    /// </summary>
    public class AggregatePath : DotnetEx.ValueObject {

        public AggregatePath(Aggregate aggregate) {
            Aggregate = aggregate;

            var list = new List<string>();
            var member = aggregate.Parent;
            while (member != null) {
                list.Insert(0, member.Name);
                member = member.Owner.Parent;
            }
            list.Insert(0, aggregate.GetRoot().Name);
            Value = string.Join(".", list);
        }

        public string Value { get; }
        public Aggregate Aggregate { get; }

        protected override IEnumerable<object> ValueObjectIdentifiers() {
            yield return Value;
        }
    }
}
