using System;
using System.Collections.Generic;
using System.Reflection;

namespace HalApplicationBuilder.Runtime {
    public class RootNavigation {

        public Assembly SchemaAssembly { get; init; }
        public Core.Config Config { get; init; }

        public IEnumerable<Item> GetRootNavigations() {
            var builder = new Core.Builder(SchemaAssembly, Config);
            var rootAggregates = builder.EnumerateRootAggregates();
            foreach (var aggregate in rootAggregates) {
                yield return new Item {
                    Text = aggregate.Name,
                    AspController = aggregate.Name,
                };
            }
        }

        public class Item {
            public string Text { get; internal init; }
            public string AspController { get; internal init; }
        }
    }
}
