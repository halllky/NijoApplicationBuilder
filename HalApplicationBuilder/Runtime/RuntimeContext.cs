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
            var dbContext = _service.GetRequiredService<DbContext>();
            return dbContext;
        }


        public TInstanceModel CreateInstance<TInstanceModel>() {
            return (TInstanceModel)CreateInstance(typeof(TInstanceModel));
        }
        public object CreateInstance(Type runtimeType) {
            // TODO: KeyがGUIDならここで採番
            var aggregate = FindAggregateByRuntimeType(runtimeType);
            var instance = RuntimeAssembly.CreateInstance(ViewModelProvider.GetInstanceModel(aggregate).RuntimeFullName);
            return instance;
        }

        public IEnumerable<object> Search(object searchCondition) {
            yield break; // TODO
        }

        /// <summary>TODO リファクタリング課題: おそらく <see cref="EntityFramework.DbEntity"/> クラスの仕事</summary>
        public IEnumerable<object> ConvertUIToDB(object instance, object parentInstance) {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var instanceType = instance.GetType();
            var instanceItem = instanceType.IsGenericType && instanceType.GetGenericTypeDefinition() == typeof(Instance<>)
                ? instance.GetType().GetProperty(nameof(Instance<object>.Item)).GetValue(instance)
                : instance;

            // Aggregateを特定
            var aggregate = FindAggregateByRuntimeType(instanceItem.GetType());
            if (aggregate == null) throw new ArgumentException($"型 {instanceItem.GetType().Name} と対応する集約が見つからない");

            // Entityのインスタンスを生成
            var entityModel = DbSchema.GetDbEntity(aggregate);
            var entity = RuntimeAssembly.CreateInstance(entityModel.RuntimeFullName);

            // 親のPKをコピーする
            if (parentInstance != null) {
                var parentInstanceType = parentInstance.GetType();
                var parentInstanceItem = parentInstanceType.IsGenericType && parentInstanceType.GetGenericTypeDefinition() == typeof(Instance<>)
                    ? parentInstance.GetType().GetProperty(nameof(Instance<object>.Item)).GetValue(parentInstance)
                    : parentInstance;
                var parentAggregate = FindAggregateByRuntimeType(parentInstanceItem.GetType());
                var parentEntityModel = DbSchema.GetDbEntity(parentAggregate);
                foreach (var pkColumn in parentEntityModel.PKColumns) {
                    var parentPk = parentInstanceType.GetProperty(pkColumn.PropertyName);
                    var childPk = entity.GetType().GetProperty(pkColumn.PropertyName);
                    var pkValue = parentPk.GetValue(parentInstance);
                    childPk.SetValue(entity, pkValue);
                }
            }

            // instacneModelの各プロパティの値をentityにマッピング
            var set = new HashSet<object> { entity };
            foreach (var member in aggregate.Members) {
                if (member is not IInstanceConverter converter) continue;
                converter.MapUIToDB(instanceItem, entity, this, set);
            }

            return set;
        }

        public InstanceKey SaveNewInstance(object createCommand) {
            if (createCommand == null) throw new ArgumentNullException(nameof(createCommand));

            var dbEntities = ConvertUIToDB(createCommand, null);
            var dbContext = GetDbContext();
            dbContext.AddRange(dbEntities);
            dbContext.SaveChanges();

            var model = createCommand.GetType().IsGenericType && createCommand.GetType().GetGenericTypeDefinition() == typeof(Instance<>)
                    ? createCommand.GetType().GetProperty(nameof(Instance<object>.Item)).GetValue(createCommand)
                    : createCommand;
            var aggregate = FindAggregateByRuntimeType(model.GetType());
            if (aggregate == null) throw new ArgumentException($"型 {createCommand.GetType().Name} と対応する集約が見つからない");
            var instanceKey = new InstanceKey(model, aggregate);

            return instanceKey;
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
