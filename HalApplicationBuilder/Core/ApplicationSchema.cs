using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Core {
    internal class ApplicationSchema : EntityFramework.IDbSchema, AspNetMvc.IViewModelProvider {
        internal ApplicationSchema(Assembly assembly, IAggregateMemberFactory memberFactory) {
            Assembly = assembly;
            _memberFactory = memberFactory;
        }

        internal Assembly Assembly { get; }
        private readonly IAggregateMemberFactory _memberFactory;

        private HashSet<Aggregate> _cache;
        private HashSet<Aggregate> Cache {
            get {
                if (_cache == null) {
                    var rootAggregates = Assembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null)
                        .Select(type => new Aggregate(type, null, _memberFactory));

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

        public EntityFramework.DbEntity GetDbEntity(Aggregate aggregate) {
            throw new NotImplementedException();
        }

        public AspNetMvc.MvcModel GetInstanceModel(Aggregate aggregate) {
            throw new NotImplementedException();
        }
        public AspNetMvc.MvcModel GetSearchConditionModel(Aggregate aggregate) {
            throw new NotImplementedException();
        }
        public AspNetMvc.MvcModel GetSearchResultModel(Aggregate aggregate) {
            throw new NotImplementedException();
        }
    }
}
