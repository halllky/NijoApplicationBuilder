using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime {
    /// <summary>
    /// 複合キーをHTTPでやりとりするためにJSON化して取り扱う仕組み
    /// </summary>
    internal class InstanceKey : ValueObject {

        internal static InstanceKey Empty => new InstanceKey(null, Array.Empty<object>());

        internal InstanceKey(Core.Aggregate? aggregate, IReadOnlyList<object> values) {
            Aggregate = aggregate;
            ObjectValue = values.ToArray();
            StringValue = JsonSerializer.Serialize(values);
        }
        private Core.Aggregate? Aggregate { get; }
        internal string StringValue { get; }
        internal object[] ObjectValue { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Aggregate;
            yield return StringValue;
        }
        public override string ToString() {
            return $"[{Aggregate?.Name}] {StringValue}";
        }
    }
}
