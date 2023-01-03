using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Runtime {
    public class RuntimeContext {
        public RuntimeContext(Assembly schemaAssembly, Assembly runtimeAssembly, IServiceProvider service) {
            SchemaAssembly = schemaAssembly;
            RuntimeAssembly = runtimeAssembly;

            ApplicationSchema = service.GetRequiredService<Core.IApplicationSchema>();
            DbSchema = service.GetRequiredService<EntityFramework.IDbSchema>();
            ViewModelProvider = service.GetRequiredService<AspNetMvc.IViewModelProvider>();
            _service = service;
        }

        internal Assembly SchemaAssembly { get; }
        internal Assembly RuntimeAssembly { get; }
        internal Core.Config Config { get; }
        internal Core.IApplicationSchema ApplicationSchema { get; }
        internal EntityFramework.IDbSchema DbSchema { get; }
        internal AspNetMvc.IViewModelProvider ViewModelProvider { get; }

        private readonly IServiceProvider _service;

        public IEnumerable<MenuItem> GetRootNavigations() {
            var rootAggregates = ApplicationSchema.RootAggregates();
            foreach (var aggregate in rootAggregates) {
                yield return new MenuItem {
                    LinkText = aggregate.Name,
                    AspController = aggregate.Name,
                };
            }
        }

        private Dictionary<string, Core.Aggregate> _typeFullNameIndex;
        public Core.Aggregate FindAggregate(object instance) {
            return FindAggregateByRuntimeType(instance?.GetType());
        }
        public Core.Aggregate FindAggregateByName(string name) {
            return ApplicationSchema.AllAggregates().SingleOrDefault(a => a.Name == name);
        }
        public Core.Aggregate FindAggregateByRuntimeType(Type runtimeType) {
            if (runtimeType == null) return null;

            // 型名と集約定義の対応関係の紐付けのキャッシュを作成
            if (_typeFullNameIndex == null) {
                _typeFullNameIndex = ApplicationSchema
                    .AllAggregates()
                    .SelectMany(
                        aggregate => new[]
                        {
                            DbSchema.GetDbEntity(aggregate).RuntimeFullName,
                            ViewModelProvider.GetSearchConditionModel(aggregate).RuntimeFullName,
                            ViewModelProvider.GetSearchResultModel(aggregate).RuntimeFullName,
                            ViewModelProvider.GetInstanceModel(aggregate).RuntimeFullName,
                        },
                        (aggregate, runtimeFullname) => new { aggregate, runtimeFullname })
                    .ToDictionary(x => x.runtimeFullname, x => x.aggregate);
            }

            return _typeFullNameIndex.GetValueOrDefault(runtimeType.FullName);
        }

        internal DbContext GetDbContext() {
            var config = _service.GetRequiredService<Core.Config>();
            var dbContext = RuntimeAssembly.CreateInstance($"{config.DbContextNamespace}.{config.DbContextName}"); ;
            return (DbContext)dbContext;
        }


        public TInstanceModel CreateInstance<TInstanceModel>() {
            return (TInstanceModel)CreateInstance(typeof(TInstanceModel));
        }
        public object CreateInstance(Type runtimeType) {
            var aggregate = FindAggregateByRuntimeType(runtimeType);
            var instance = RuntimeAssembly.CreateInstance(ViewModelProvider.GetInstanceModel(aggregate).RuntimeFullName);
            return instance;
        }

        public IEnumerable<object> Search(object searchCondition) {
            yield break; // TODO
        }

        public InstanceKey SaveNewInstance(object createCommand) {
            var aggregate = FindAggregate(createCommand);
            return new InstanceKey(Guid.NewGuid().ToString(), aggregate); // TODO
        }

        public object FindInstance(InstanceKey key) {
            return null; // TODO
        }

        public void UpdateInstance(object instance) {
            // TODO
        }

        public void DeleteInstance(object instance) {
            // TODO
        }
    }
}
