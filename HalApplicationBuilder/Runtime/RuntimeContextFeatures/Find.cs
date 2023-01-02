﻿using System;
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
            var tables = new List<Core.AutoGenerateDbEntityClass>();
            tables.Add(key.Aggregate.ToDbTableModel());
            tables.AddRange(key.Aggregate
                .GetDescendants()
                .Select(aggregate => aggregate.ToDbTableModel()));

            var rootInstance = RuntimeContext
                .RuntimeAssembly
                .CreateInstance(key.Aggregate.InstanceModel.RuntimeFullName);

            throw new NotImplementedException();
        }
    }
}