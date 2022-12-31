using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core {
    internal class ApplicationSchema {
        internal ApplicationSchema(Assembly assembly, Config config) {
            Assembly = assembly;
            Config = config;
        }

        internal Assembly Assembly { get; }
        internal Config Config { get; }

        private HashSet<Aggregate> _cache;
        private HashSet<Aggregate> Cache {
            get {
                if (_cache == null) {
                    var rootAggregates = Assembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null)
                        .Select(type => new Aggregate {
                            Schema = this,
                            UnderlyingType = type,
                            Parent = null,
                        });

                    _cache = new HashSet<Aggregate>();
                    foreach (var aggregate in rootAggregates) {
                        _cache.Add(aggregate);

                        foreach (var descendant in aggregate.GetDescendants()) {
                            _cache.Add(descendant);
                        }
                    }
                }
                return _cache;
            }
        }

        internal IEnumerable<Aggregate> AllAggregates() {
            return Cache;
        }
        internal IEnumerable<Aggregate> RootAggregates() {
            return Cache.Where(a => a.Parent == null);
        }
        internal Aggregate FindByType(Type type) {
            return Cache.SingleOrDefault(a => a.UnderlyingType == type);
        }
    }
}
