using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core {
    internal class Builder {
        internal Builder(Assembly assembly, Config config) {
            _assembly = assembly;
            _config = config;
        }

        private readonly Assembly _assembly;
        private readonly Config _config;

        private HashSet<Aggregate> _allAggregates;
        private HashSet<Aggregate> AllAggregates {
            get {
                if (_allAggregates == null) {
                    var rootAggregates = _assembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null)
                        .Select(type => new Aggregate {
                            Config = _config,
                            UnderlyingType = type,
                            Parent = null,
                        });

                    _allAggregates = new HashSet<Aggregate>();
                    foreach (var aggregate in rootAggregates) {
                        _allAggregates.Add(aggregate);

                        foreach (var descendant in aggregate.GetDescendants()) {
                            _allAggregates.Add(descendant);
                        }
                    }
                }
                return _allAggregates;
            }
        }

        internal IEnumerable<Aggregate> EnumerateAllAggregates() {
            return AllAggregates;
        }
        internal IEnumerable<Aggregate> EnumerateRootAggregates() {
            return AllAggregates.Where(a => a.Parent == null);
        }
    }
}
