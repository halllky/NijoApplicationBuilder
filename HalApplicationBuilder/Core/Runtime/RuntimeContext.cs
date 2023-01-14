using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.UIModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Core.Runtime {
    public class RuntimeContext {
        public RuntimeContext(Assembly schemaAssembly, Assembly runtimeAssembly, IServiceProvider service) {
            SchemaAssembly = schemaAssembly;
            RuntimeAssembly = runtimeAssembly;

            ApplicationSchema = service.GetRequiredService<Core.IApplicationSchema>();
            DbSchema = service.GetRequiredService<Core.DBModel.IDbSchema>();
            ViewModelProvider = service.GetRequiredService<IViewModelProvider>();
            _service = service;
        }

        public string ApplicationName => "サンプルアプリケーション";

        internal Assembly SchemaAssembly { get; }
        internal Assembly RuntimeAssembly { get; }
        internal Core.Config Config { get; }
        internal Core.IApplicationSchema ApplicationSchema { get; }
        internal Core.DBModel.IDbSchema DbSchema { get; }
        internal Core.UIModel.IViewModelProvider ViewModelProvider { get; }

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
            var aggregate = FindAggregateByRuntimeType(searchCondition.GetType());

            // クエリの組み立て
            var converters = aggregate.Members
                .Select(member => member as IInstanceConverter)
                .Where(member => member != null)
                .ToArray();
            var paramGenerator = _service.GetRequiredService<Core.DBModel.SelectStatement.IParamGenerator>();
            var selectStatement = new Core.DBModel.SelectStatement(paramGenerator);
            var mainTable = DbSchema.GetDbEntity(aggregate);
            selectStatement.From(mainTable);
            foreach (var member in converters) {
                member.BuildSelectStatement(selectStatement, searchCondition, this, string.Empty);
            }

            // クエリ実行
            var conn = GetDbContext().Database.GetDbConnection();
            var cmd = selectStatement.ToSqlCommand(conn);
            conn.Open();
            var reader = cmd.ExecuteReader();

            // クエリ結果をUIの型にマッピング
            var searchResultTypeName = ViewModelProvider.GetSearchResultModel(aggregate).RuntimeFullName;
            var searchResultType = RuntimeAssembly.GetType(searchResultTypeName);
            while (reader.Read()) {
                var row = Activator.CreateInstance(searchResultType);
                foreach (var member in converters) {
                    member.MapSearchResultToUI(reader, row, this, string.Empty);
                }
                yield return row;
            }
        }

        public bool SaveNewInstance(object createCommand, out InstanceKey instanceKey, out ICollection<string> errors) {
            if (createCommand == null) throw new ArgumentNullException(nameof(createCommand));

            var aggregate = FindAggregateByRuntimeType(createCommand.GetType());
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var dbInstance = dbEntity.ConvertUiInstanceToDbInstance(createCommand, this);
            var dbContext = GetDbContext();

            try {
                dbContext.Add(dbInstance);
                dbContext.SaveChanges();
            } catch (InvalidOperationException ex) {
                errors = new[] { ex.Message };
                instanceKey = null;
                return false;
            } catch (DbUpdateException ex) {
                errors = new[] { ex.Message };
                instanceKey = null;
                return false;
            }

            instanceKey = InstanceKey.Create(dbInstance, dbEntity);
            errors = Array.Empty<string>();
            return true;
        }

        public TInstanceModel FindInstance<TInstanceModel>(InstanceKey pk) {
            var entityType = RuntimeAssembly.GetType(pk.DbEntity.RuntimeFullName);
            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, pk.Values);
            var uiInstance = pk.DbEntity.ConvertDbInstanceToUiInstance(dbInstance, this);
            return (TInstanceModel)uiInstance;
        }

        public bool UpdateInstance(object uiInstance, out InstanceKey instanceKey, out ICollection<string> errors) {
            // 検索用の主キーを作成する
            var aggregate = FindAggregateByRuntimeType(uiInstance.GetType());
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var tempDbInstance = dbEntity.ConvertUiInstanceToDbInstance(uiInstance, this);
            instanceKey = InstanceKey.Create(tempDbInstance, dbEntity);
            // DBからentityを読み込む
            var entityType = RuntimeAssembly.GetType(dbEntity.RuntimeFullName);
            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, instanceKey.Values);
            // UIの値をentityにマッピングする
            dbEntity.MapUiInstanceToDbInsntace(uiInstance, dbInstance, this);

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

        public void DeleteInstance(object instance) {
            // TODO
        }

        public IEnumerable<AutoCompleteSource> AutoComplete(Core.Aggregate aggregate, string inputText) {
            var paramGenerator = _service.GetRequiredService<Core.DBModel.SelectStatement.IParamGenerator>();
            var selectStatement = new Core.DBModel.SelectStatement(paramGenerator);

            throw new NotImplementedException();
        }
    }
}
