using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Runtime.RuntimeContextFeatures {
    internal class Find {
        internal Find(RuntimeContext context) {
            RuntimeContext = context;
        }

        private RuntimeContext RuntimeContext { get; }

        internal TInstanceModel Execute<TInstanceModel>(InstanceKey key) {
            // SQL発行回数が全く最適化されていないが後で考える
            var tables = new List<EntityFramework.DbEntity>();
            tables.Add(RuntimeContext.DbSchema.GetDbEntity(key.Aggregate));
            tables.AddRange(key.Aggregate
                .GetDescendants()
                .Select(aggregate => RuntimeContext.DbSchema.GetDbEntity(aggregate)));

            var rootInstance = RuntimeContext
                .RuntimeAssembly
                .CreateInstance(RuntimeContext.ViewModelProvider.GetInstanceModel(key.Aggregate).RuntimeFullName);

            throw new NotImplementedException();
        }
    }
}
