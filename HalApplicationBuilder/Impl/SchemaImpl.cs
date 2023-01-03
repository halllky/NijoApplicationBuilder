using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Impl {
    internal class SchemaImpl :
        Core.IApplicationSchema,
        EntityFramework.IDbSchema,
        AspNetMvc.IViewModelProvider {

        internal SchemaImpl(Assembly assembly, IServiceProvider serviceProvider) {
            _schemaAssembly = assembly;
            _services = serviceProvider;
        }

        private readonly Assembly _schemaAssembly;
        private readonly IServiceProvider _services;


        #region ApplicationSchema
        private HashSet<Core.Aggregate> _appSchema;
        private IReadOnlySet<Core.Aggregate> AppSchema {
            get {
                if (_appSchema == null) {
                    var memberFactory = _services.GetRequiredService<Core.IAggregateMemberFactory>();
                    var rootAggregates = _schemaAssembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null)
                        .Select(type => new Core.Aggregate(type, null, memberFactory));

                    _appSchema = new HashSet<Core.Aggregate>();
                    foreach (var aggregate in rootAggregates) {
                        _appSchema.Add(aggregate);

                        foreach (var descendant in aggregate.GetDescendants()) {
                            _appSchema.Add(descendant);
                        }
                    }
                }
                return _appSchema;
            }
        }

        public IEnumerable<Core.Aggregate> AllAggregates() {
            return AppSchema;
        }
        public IEnumerable<Core.Aggregate> RootAggregates() {
            return AppSchema.Where(a => a.Parent == null);
        }
        public Core.Aggregate FindByType(Type type) {
            return AppSchema.SingleOrDefault(a => a.UnderlyingType == type);
        }
        #endregion ApplicationSchema


        #region DbSchema
        private Dictionary<Core.Aggregate, EntityFramework.DbEntity> _dbEntities;
        private IReadOnlyDictionary<Core.Aggregate, EntityFramework.DbEntity> DbEntities {
            get {
                if (_dbEntities == null) {
                    var config = _services.GetRequiredService<Core.Config>();
                    var aggregates = AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();
                    _dbEntities = new Dictionary<Core.Aggregate, EntityFramework.DbEntity>();
                    foreach (var aggregate in aggregates) {
                        var parent = aggregate.Parent == null
                            ? null
                            : _dbEntities[aggregate.Parent.Owner];
                        _dbEntities.Add(aggregate, new EntityFramework.DbEntity(aggregate, parent, config));
                    }
                }
                return _dbEntities;
            }
        }

        public EntityFramework.DbEntity GetDbEntity(Core.Aggregate aggregate) {
            return DbEntities[aggregate];
        }
        #endregion DbSchema


        #region ViewModelProvider
        private Dictionary<Core.Aggregate, AspNetMvc.MvcModel> _searchConditions;
        private Dictionary<Core.Aggregate, AspNetMvc.MvcModel> _searchResults;
        private Dictionary<Core.Aggregate, AspNetMvc.MvcModel> _instanceModels;

        private IReadOnlyDictionary<Core.Aggregate, AspNetMvc.MvcModel> SearchConditions {
            get {
                if (_searchConditions == null) {

                    var config = _services.GetRequiredService<Core.Config>();
                    var aggregates = AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();

                    _searchConditions = new Dictionary<Core.Aggregate, AspNetMvc.MvcModel>();
                    foreach (var aggregate in aggregates) {
                        _searchConditions.Add(aggregate, new AspNetMvc.SearchConditionClass {
                            Source = aggregate,
                            Config = config,
                        });
                    }
                }
                return _searchConditions;
            }
        }
        private IReadOnlyDictionary<Core.Aggregate, AspNetMvc.MvcModel> SearchResults {
            get {
                if (_searchResults == null) {

                    var config = _services.GetRequiredService<Core.Config>();
                    var aggregates = AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();

                    _searchResults = new Dictionary<Core.Aggregate, AspNetMvc.MvcModel>();
                    foreach (var aggregate in aggregates) {
                        _searchResults.Add(aggregate, new AspNetMvc.SearchResultClass {
                            Source = aggregate,
                            Config = config,
                        });
                    }
                }
                return _searchResults;
            }
        }
        private IReadOnlyDictionary<Core.Aggregate, AspNetMvc.MvcModel> InstanceModels {
            get {
                if (_instanceModels == null) {

                    var config = _services.GetRequiredService<Core.Config>();
                    var aggregates = AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();

                    _instanceModels = new Dictionary<Core.Aggregate, AspNetMvc.MvcModel>();
                    foreach (var aggregate in aggregates) {
                        _instanceModels.Add(aggregate, new AspNetMvc.InstanceModelClass {
                            Source = aggregate,
                            Config = config,
                        });
                    }
                }
                return _instanceModels;
            }
        }

        public AspNetMvc.MvcModel GetInstanceModel(Core.Aggregate aggregate) {
            return InstanceModels[aggregate];
        }
        public AspNetMvc.MvcModel GetSearchConditionModel(Core.Aggregate aggregate) {
            return SearchConditions[aggregate];
        }
        public AspNetMvc.MvcModel GetSearchResultModel(Core.Aggregate aggregate) {
            return SearchResults[aggregate];
        }
        #endregion ViewModelProvider
    }
}
