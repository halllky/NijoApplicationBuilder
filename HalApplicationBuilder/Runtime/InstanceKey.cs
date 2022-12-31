using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// 複合キーをASP.NETで扱えるようにするための仕組み
    /// </summary>
    public class InstanceKey {
        internal InstanceKey(string stringValue, Core.Aggregate aggregate) {
            Aggregate = aggregate;
            StringValue = stringValue;
        }

        public Core.Aggregate Aggregate { get; }

        public string StringValue { get; }
        public IReadOnlyDictionary<Core.AggregateMemberBase, object> ParsedValue { get; }
    }
}
