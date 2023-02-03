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
        internal RuntimeContext(Assembly runtimeAssembly, IServiceProvider service) {
            RuntimeAssembly = runtimeAssembly;

            ApplicationSchema = service.GetRequiredService<Core.IApplicationSchema>();
            DbSchema = service.GetRequiredService<Core.DBModel.IDbSchema>();
            ViewModelProvider = service.GetRequiredService<IViewModelProvider>();
            _service = service;
        }

        public string ApplicationName => "サンプルアプリケーション";

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
        public Core.Aggregate FindAggregateByGuid(Guid guid) {
            return ApplicationSchema.AllAggregates().SingleOrDefault(a => a.GUID == guid);
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
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var scModel = ViewModelProvider.GetSearchConditionModel(aggregate);
            var srModel = ViewModelProvider.GetSearchResultModel(aggregate);
            var methodName = new EntityFramework.SearchMethodRenderer.Search(dbEntity, scModel, srModel, Config).MethodName;

            var dbContext = GetDbContext();
            var method = dbContext.GetType().GetMethod(methodName);

            var searchResult = (IEnumerable)method.Invoke(dbContext, new object[] { searchCondition });
            foreach (var item in searchResult) {
                yield return item;
            }
        }

        public bool SaveNewInstance(object createCommand, out InstanceKey instanceKey, out ICollection<string> errors) {
            if (createCommand == null) throw new ArgumentNullException(nameof(createCommand));

            var aggregate = FindAggregateByRuntimeType(createCommand.GetType());
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var dbInstance = ConvertToDbInstance(createCommand, dbEntity);
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
            var uiInstance = ConvertToUiInstance(dbInstance, pk.DbEntity);
            return (TInstanceModel)uiInstance;
        }

        public bool UpdateInstance(object uiInstance, out InstanceKey instanceKey, out ICollection<string> errors) {
            // 検索用の主キーを作成する
            var aggregate = FindAggregateByRuntimeType(uiInstance.GetType());
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var tempDbInstance = ConvertToDbInstance(uiInstance, dbEntity);
            instanceKey = InstanceKey.Create(tempDbInstance, dbEntity);
            // DBからentityを読み込む
            var entityType = RuntimeAssembly.GetType(dbEntity.RuntimeFullName);
            var dbContext = GetDbContext();
            var dbInstance = dbContext.Find(entityType, instanceKey.Values);
            // UIの値をentityにマッピングする
            var mapper = new MemberMapperFromUiToDb(uiInstance, dbInstance, this);
            foreach (var member in dbEntity.Source.Members) {
                member.Accept(mapper);
            }

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

        public IEnumerable<AutoCompleteSource> LoadAutoCompleteDataSource(Aggregate aggregate, string keyword) {
            var dbEntity = DbSchema.GetDbEntity(aggregate);
            var methodName = new EntityFramework.AutoCompleteSourceMethodRenderer.LoadAutoComplele(dbEntity, Config).MethodName;
            var dbContext = GetDbContext();
            var method = dbContext.GetType().GetMethod(methodName);
            var result = (IEnumerable)method.Invoke(dbContext, new object[] { keyword });

            foreach (var item in result) {
                yield return new AutoCompleteSource {
                    InstanceKey = InstanceKey.Create(item, dbEntity).StringValue,
                    InstanceName = InstanceName.Create(item, dbEntity).Value,
                };
            }
        }

        #region Instance Converting
        internal object ConvertToDbInstance(object uiInstance, DBModel.DbEntity dbEntity) {
            var dbInstance = RuntimeAssembly.CreateInstance(dbEntity.RuntimeFullName);
            var mapper = new MemberMapperFromUiToDb(uiInstance, dbInstance, this);
            foreach (var member in dbEntity.Source.Members) {
                member.Accept(mapper);
            }
            return dbInstance;
        }
        internal object ConvertToUiInstance(object dbInstance, DBModel.DbEntity dbEntity) {
            var uiModel = ViewModelProvider.GetInstanceModel(dbEntity.Source);
            var uiInstance = RuntimeAssembly.CreateInstance(uiModel.RuntimeFullName);
            var mapper = new MemberMapperFromDbToUi(dbInstance, uiInstance, this);
            foreach (var member in dbEntity.Source.Members) {
                member.Accept(mapper);
            }
            return uiInstance;
        }
        #endregion
    }
}
