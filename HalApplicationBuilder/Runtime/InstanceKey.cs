using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// 複合キーをASP.NETで扱えるようにするための仕組み
    /// </summary>
    public class InstanceKey {
        internal InstanceKey(object instanceModel, Core.Aggregate aggregate) {
            throw new NotImplementedException();
        }

        public Core.Aggregate Aggregate { get; }

        public string StringValue { get; }
        public IReadOnlyDictionary<EntityFramework.DbColumn, object> ParsedValue { get; }
    }
}
