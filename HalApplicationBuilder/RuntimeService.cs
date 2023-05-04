using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder {

    /// <summary>
    /// HalApplicationBuilderの実行時機能を提供します。
    /// </summary>
    public sealed class RuntimeService : Runtime.IInstanceConvertingContext {
        /// <summary>
        /// <see cref="IServiceCollection"/> に実行時サービスを登録します。
        /// </summary>
        /// <param name="serviceCollection">サービスコレクションのインスタンス</param>
        /// <param name="runtimeAssembly">実行時アセンブリ</param>
        /// <param name="halappJsonPath">コード自動生成時に生成された設定ファイルのパス</param>
        public static void Configure(IServiceCollection serviceCollection, Assembly runtimeAssembly, string halappJsonPath) {
            serviceCollection.AddScoped(_ => {
                using var stream = new FileStream(halappJsonPath, FileMode.Open, FileAccess.Read);
                var schema = System.Text.Json.JsonSerializer.Deserialize<Serialized.AppSchemaJson>(stream);
                if (schema == null) throw new InvalidOperationException();
                return schema;
            });
            serviceCollection.AddScoped(provider => {
                var schema = provider.GetRequiredService<Serialized.AppSchemaJson>();
                return Core.Config.FromJson(schema);
            });
            serviceCollection.AddScoped(provider => {
                var schema = provider.GetRequiredService<Serialized.AppSchemaJson>();
                var config = provider.GetRequiredService<Core.Config>();
                var rootAggregates = Core.Definition.JsonDefine
                    .Create(config, schema)
                    .Select(def => new RootAggregate(config, def));
                return new RuntimeService(provider, runtimeAssembly, rootAggregates);
            });
        }

        internal RuntimeService(IServiceProvider serviceProvider, Assembly runtimeAssembly, IEnumerable<RootAggregate> rootAggregates) {
            _service = serviceProvider;
            _runtimeAssembly = runtimeAssembly;
            _rootAggregates = rootAggregates;
        }

        public string ApplicationName => "サンプルアプリケーション";

        private readonly IServiceProvider _service;
        private readonly Assembly _runtimeAssembly;
        private readonly IEnumerable<RootAggregate> _rootAggregates;

        private IEnumerable<Aggregate> GetAllAggregates() {
            foreach (var root in _rootAggregates) {
                yield return root;
                foreach (var descendant in root.GetDescendants()) {
                    yield return descendant;
                }
            }
        }
        private DbContext GetDbContext() {
            var dbContext = _service.GetRequiredService<DbContext>();
            return dbContext;
        }

        internal Core.RootAggregate? FindRootAggregate(Type runtimeType) {
            return _rootAggregates.SingleOrDefault(a => {
                if (runtimeType.FullName == a.ToUiInstanceClass().CSharpTypeName) return true;
                if (runtimeType.FullName == a.ToSearchResultClass().CSharpTypeName) return true;
                if (runtimeType.FullName == a.ToSearchConditionClass().CSharpTypeName) return true;
                return false;
            });
        }
        internal Core.Aggregate? FindAggregate(Type runtimeType) {
            return GetAllAggregates().SingleOrDefault(a => {
                if (runtimeType.FullName == a.ToUiInstanceClass().CSharpTypeName) return true;
                if (runtimeType.FullName == a.ToSearchResultClass().CSharpTypeName) return true;
                if (runtimeType.FullName == a.ToSearchConditionClass().CSharpTypeName) return true;
                return false;
            });
        }
        internal Core.Aggregate? FindAggregate(string aggregateTreePath) {
            return GetAllAggregates().SingleOrDefault(a => {
                if (aggregateTreePath == a.GetUniquePath()) return true;
                return false;
            });
        }
        internal Core.Aggregate? FindAggregate(Guid aggregateGuid) {
            return GetAllAggregates().SingleOrDefault(a => {
                if (aggregateGuid == a.GetGuid()) return true;
                return false;
            });
        }

        public IEnumerable<Runtime.MenuItem> GetRootNavigations() {
            return _rootAggregates.Select(aggregate => new Runtime.MenuItem {
                LinkText = aggregate.GetDisplayName(),
                AspController = aggregate.GetDisplayName(),
            });
        }

        public object CreateInstance(string typeName) {
            var instance = _runtimeAssembly.CreateInstance(typeName);
            if (instance == null) throw new ArgumentException($"実行時アセンブリ内に型 {typeName} が存在しません。");
            return instance;
        }
        public object CreateSearchCondition(Type searchConditionType) {
            return Activator.CreateInstance(searchConditionType)!;
        }
        public object CreateUIInstance(Type uiInstanceType) {
            // TODO: KeyがGUIDならここで採番
            return Activator.CreateInstance(uiInstanceType)!;
        }
        public object CreateUIInstance(string aggregateTreePath) {
            var aggregate = GetAllAggregates().Single(a => a.GetUniquePath() == aggregateTreePath);
            if (aggregate == null) throw new ArgumentException($"{aggregateTreePath} と対応する集約が見つかりません。");

            var typeName = aggregate.ToUiInstanceClass().CSharpTypeName;
            var type = _runtimeAssembly.GetType(typeName);
            if (type == null) throw new ArgumentException($"実行時アセンブリ内に型 {typeName} が存在しません。");

            return CreateUIInstance(type);
        }

        public IEnumerable Search<TSearchCondition>(TSearchCondition? searchCondition) {
            var aggregate = FindRootAggregate(typeof(TSearchCondition));
            if (aggregate == null) throw new ArgumentException($"型 {typeof(TSearchCondition).Name} と対応する集約が見つかりません。");

            var dbContext = GetDbContext();
            var method = aggregate.GetSearchMethod(_runtimeAssembly, dbContext);
            var param = searchCondition ?? CreateSearchCondition(typeof(TSearchCondition));

            var searchResult = (IEnumerable)method.Invoke(dbContext, new object[] { param })!;

            foreach (var item in searchResult) {
                ((Runtime.SearchResultBase)item).__halapp__InstanceKey = aggregate.CreateInstanceKeyFromSearchResult(item).StringValue;
                yield return item;
            }
        }

        public bool TrySaveNewInstance<TUIInstance>(TUIInstance uiInstance, out string instanceKey, out ICollection<string> errors) where TUIInstance : Runtime.UIInstanceBase {
            var aggregate = FindRootAggregate(typeof(TUIInstance));
            if (aggregate == null) throw new ArgumentException($"型 {typeof(TUIInstance).Name} と対応する集約が見つかりません。");

            var dbInstance = CreateInstance(aggregate.ToDbEntity().CSharpTypeName);
            aggregate.MapUiToDb(uiInstance, dbInstance, this);
            var dbContext = GetDbContext();

            try {
                dbContext.Add(dbInstance);
                dbContext.SaveChanges();
            } catch (InvalidOperationException ex) {
                errors = ex.GetMessagesRecursively().ToArray();
                instanceKey = string.Empty;
                return false;
            } catch (DbUpdateException ex) {
                errors = ex.GetMessagesRecursively().ToArray();
                instanceKey = string.Empty;
                return false;
            }

            instanceKey = aggregate.CreateInstanceKeyFromUiInstnace(uiInstance).StringValue;
            errors = Array.Empty<string>();
            return true;
        }

        public TUIInstance? FindInstance<TUIInstance>(string instanceKey, out string instanceName) {
            var key = Runtime.InstanceKey.FromSerializedString(instanceKey);
            if (key == null) throw new ArgumentException($"文字列 '{instanceKey}' から検索キーを取得できません。");

            var aggregate = FindRootAggregate(typeof(TUIInstance));
            if (aggregate == null) throw new ArgumentException($"型 {typeof(TUIInstance).Name} と対応する集約が見つかりません。");

            var entityTypeName = aggregate.ToDbEntity().CSharpTypeName;
            var entityType = _runtimeAssembly.GetType(entityTypeName);
            if (entityType == null) throw new ArgumentException($"実行時アセンブリ内に型 {entityTypeName} が存在しません。");

            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, key.GetFlattenObjectValues());
            if (dbInstance == null) {
                instanceName = string.Empty;
                return default;
            }

            instanceName = Runtime.InstanceName.Create(dbInstance, aggregate).Value;

            var uiInstance = CreateInstance(aggregate.ToUiInstanceClass().CSharpTypeName);
            aggregate.MapDbToUi(dbInstance, uiInstance, this);
            return (TUIInstance)uiInstance;
        }

        public bool TryUpdate<TUIInstance>(TUIInstance uiInstance, out string instanceKey, out ICollection<string> errors) where TUIInstance : Runtime.UIInstanceBase {
            var aggregate = FindRootAggregate(typeof(TUIInstance));
            if (aggregate == null) throw new ArgumentException($"型 {typeof(TUIInstance).Name} と対応する集約が見つかりません。");

            // Find
            var key = aggregate.CreateInstanceKeyFromUiInstnace(uiInstance);
            instanceKey = key.StringValue;

            var entityTypeName = aggregate.ToDbEntity().CSharpTypeName;
            var entityType = _runtimeAssembly.GetType(entityTypeName);
            if (entityType == null) throw new ArgumentException($"実行時アセンブリ内に型 {entityTypeName} が存在しません。");

            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, key.ObjectValue);
            if (dbInstance == null) {
                errors = new[] { "更新対象のデータが見つかりません。" };
                return false;
            }

            // Update
            aggregate.MapUiToDb(uiInstance, dbInstance, this);

            // Save
            try {
                dbContext.Update(dbInstance);
                dbContext.SaveChanges();
            } catch (InvalidOperationException ex) {
                errors = new[] { ex.Message };
                return false;
            } catch (DbUpdateException ex) {
                errors = new[] { ex.Message };
                return false;
            }

            errors = Array.Empty<string>();
            return true;
        }

        public void DeleteInstance<TUIInstance>(TUIInstance uiInstance) where TUIInstance : Runtime.UIInstanceBase {
            // TODO
        }
    }
}
