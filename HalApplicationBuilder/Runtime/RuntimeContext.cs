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

        public string ApplicationName => "サンプルアプリケーション";

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
        /// <summary>見つからない場合は例外</summary>
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

            if (!_typeFullNameIndex.ContainsKey(runtimeType.FullName))
                throw new ArgumentException($"型 {runtimeType.Name} と対応する集約が見つからない");

            return _typeFullNameIndex[runtimeType.FullName];
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

        public InstanceKey SaveNewInstance(object createCommand) {
            if (createCommand == null) throw new ArgumentNullException(nameof(createCommand));

            var aggregate = FindAggregateByRuntimeType(createCommand.GetType());
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var dbInstance = dbEntity.ConvertUiInstanceToDbInstance(createCommand, this);

            var dbContext = GetDbContext();
            dbContext.Add(dbInstance);
            dbContext.SaveChanges();

            var instanceKey = InstanceKey.Create(dbInstance, dbEntity);
            return instanceKey;
        }

        public TInstanceModel FindInstance<TInstanceModel>(InstanceKey pk) {
            var entityType = RuntimeAssembly.GetType(pk.DbEntity.RuntimeFullName);
            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, pk.Values);
            var uiInstance = pk.DbEntity.ConvertDbInstanceToUiInstance(dbInstance, this);
            return (TInstanceModel)uiInstance;
        }

        public InstanceKey UpdateInstance(object uiInstance) {
            // 検索用の主キーを作成する
            var aggregate = FindAggregateByRuntimeType(uiInstance.GetType());
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var tempDbInstance = dbEntity.ConvertUiInstanceToDbInstance(uiInstance, this);
            var pk = InstanceKey.Create(tempDbInstance, dbEntity);
            // DBからentityを読み込む
            var entityType = RuntimeAssembly.GetType(dbEntity.RuntimeFullName);
            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, pk.Values);
            // UIの値をentityにマッピングする
            dbEntity.MapUiInstanceToDbInsntace(uiInstance, dbInstance, this);
            // Save
            dbContext.Update(dbInstance);
            dbContext.SaveChanges();

            return pk;
        }

        public void DeleteInstance(object instance) {
            // TODO
        }
    }
}
