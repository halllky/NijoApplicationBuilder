using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core.DBModel;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Impl {
    internal class SchemaImpl :
        Core.IApplicationSchema,
        IDbSchema,
        IViewModelProvider {

        internal SchemaImpl(Assembly assembly, IServiceProvider serviceProvider) {
            _schemaAssembly = assembly;
            _services = serviceProvider;
        }

        private readonly Assembly _schemaAssembly;
        private readonly IServiceProvider _services;


        #region ApplicationSchema
        private HashSet<Core.Aggregate> _appSchema;
        private Dictionary<string, Core.Aggregate> _pathMapping;
        private IReadOnlySet<Core.Aggregate> AppSchema {
            get {
                if (_appSchema == null) BuildAggregates();
                return _appSchema;
            }
        }
        private IReadOnlyDictionary<string, Core.Aggregate> PathMapping {
            get {
                if (_pathMapping == null) BuildAggregates();
                return _pathMapping;
            }
        }
        private void BuildAggregates() {
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

            _pathMapping = _appSchema
                .GroupBy(aggregate => new Core.AggregatePath(aggregate))
                .ToDictionary(path => path.Key.Value, path => path.First());
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
        public Core.Aggregate FindByPath(string aggregatePath) {
            return PathMapping[aggregatePath];
        }
        #endregion ApplicationSchema


        #region DbSchema
        private Dictionary<Core.Aggregate, DbEntity> _dbEntities;
        private IReadOnlyDictionary<Core.Aggregate, DbEntity> DbEntities {
            get {
                if (_dbEntities == null) {
                    var config = _services.GetRequiredService<Core.Config>();
                    var aggregates = AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();
                    _dbEntities = new Dictionary<Core.Aggregate, DbEntity>();
                    foreach (var aggregate in aggregates) {
                        var parent = aggregate.Parent == null
                            ? null
                            : _dbEntities[aggregate.Parent.Owner];
                        var child = new DbEntity(aggregate, parent, config);
                        _dbEntities.Add(aggregate, child);
                        parent?.children.Add(child);
                    }
                }
                return _dbEntities;
            }
        }

        public DbEntity GetDbEntity(Core.Aggregate aggregate) {
            return DbEntities[aggregate];
        }
        #endregion DbSchema


        #region ViewModelProvider
        private Dictionary<Core.Aggregate, MvcModel> _searchConditions;
        private Dictionary<Core.Aggregate, MvcModel> _searchResults;
        private Dictionary<Core.Aggregate, MvcModel> _instanceModels;

        private IReadOnlyDictionary<Core.Aggregate, MvcModel> SearchConditions {
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
        private IReadOnlyDictionary<Core.Aggregate, MvcModel> SearchResults {
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
        private IReadOnlyDictionary<Core.Aggregate, MvcModel> InstanceModels {
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

        public MvcModel GetInstanceModel(Core.Aggregate aggregate) {
            return InstanceModels[aggregate];
        }
        public MvcModel GetSearchConditionModel(Core.Aggregate aggregate) {
            return SearchConditions[aggregate];
        }
        public MvcModel GetSearchResultModel(Core.Aggregate aggregate) {
            return SearchResults[aggregate];
        }
        #endregion ViewModelProvider
    }
}
