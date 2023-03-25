using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyInjection;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Runtime.AspNetMvc;

namespace HalApplicationBuilder
{
    public class HalApp {

        #region Configure
        public static void Configure(IServiceCollection serviceCollection, Config config, Type[] rootAggregateTypes) {
            Configure(
                serviceCollection,
                null,
                config,
                () => rootAggregateTypes.Select(t => new RootAggregate(config, IAggregateSetting.FromReflection(config, t))));
        }
        public static void Configure(IServiceCollection serviceCollection, Config config, Assembly assembly, string? @namespace = null) {
            Configure(serviceCollection, null, config, assembly, @namespace);
        }
        public static void Configure(IServiceCollection serviceCollection, Assembly? runtimeAssembly, Config config, Assembly assembly, string? @namespace = null) {
            Configure(
                serviceCollection,
                runtimeAssembly,
                config,
                () => {
                    var types = assembly
                        .GetTypes()
                        .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null);
                    if (!string.IsNullOrWhiteSpace(@namespace)) {
                        types = types.Where(type => type.Namespace?.StartsWith(@namespace) == true);
                    }
                    return types.Select(t => new RootAggregate(config, IAggregateSetting.FromReflection(config, t)));
                });

        }
        private static void Configure(IServiceCollection serviceCollection, Assembly? runtimeAssembly, Config config, Func<IEnumerable<RootAggregate>> rootAggregateBuidler) {
            serviceCollection.AddScoped(_ => config);
            serviceCollection.AddScoped(provider => {
                var rootAggregates = rootAggregateBuidler().ToArray();
                return new HalApp(provider, rootAggregates);
            });
            if (runtimeAssembly != null) {
                serviceCollection.AddScoped(provider => {
                    var rootAggregates = rootAggregateBuidler().ToArray();
                    return new HalApp.RuntimeService(provider, runtimeAssembly, rootAggregates);
                });
            }
        }
        #endregion Configure


        private HalApp(IServiceProvider serviceProvider, RootAggregate[] rootAggregates) {
            _services = serviceProvider;
            _rootAggregates = rootAggregates;
        }

        private readonly IServiceProvider _services;
        private readonly RootAggregate[] _rootAggregates;


        #region CodeGenerating
        /// <summary>
        /// コードの自動生成を実行します。
        /// </summary>
        public void GenerateCode(TextWriter? log = null) {

            //var validator = new AggregateValidator(_services);
            //if (validator.HasError(error => log?.WriteLine(error))) {
            //    log?.WriteLine("コード自動生成終了");
            //    return;
            //}

            log?.WriteLine($"コード自動生成開始");

            var config = _services.GetRequiredService<Config>();
            var allAggregates = _rootAggregates
                .SelectMany(a => a.GetDescendantsAndSelf())
                .ToArray();

            log?.WriteLine("コード自動生成: 集約定義");
            using (var sw = new StreamWriter(Path.Combine(config.OutProjectDir, "halapp.json"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(System.Text.Json.JsonSerializer.Serialize(_rootAggregates.Select(a => a.ToJson()).ToArray()));
            }

            var efSourceDir = Path.Combine(config.OutProjectDir, config.EntityFrameworkDirectoryRelativePath);
            if (Directory.Exists(efSourceDir)) Directory.Delete(efSourceDir, recursive: true);
            Directory.CreateDirectory(efSourceDir);

            log?.WriteLine("コード自動生成: Entity定義");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Entities.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.EntityClassTemplate(config, allAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: DbSet");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "DbSet.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.DbSetTemplate(config, allAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: OnModelCreating");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "OnModelCreating.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.OnModelCreatingTemplate(config, allAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: Search");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Search.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.SearchMethodTemplate(config, _rootAggregates).TransformText());
            }
            log?.WriteLine("コード自動生成: AutoCompleteSource");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "AutoCompleteSource.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.AutoCompleteSourceTemplate(config, allAggregates).TransformText());
            }

            var modelDir = Path.Combine(config.OutProjectDir, config.MvcModelDirectoryRelativePath);
            if (Directory.Exists(modelDir)) Directory.Delete(modelDir, recursive: true);
            Directory.CreateDirectory(modelDir);

            log?.WriteLine("コード自動生成: MVC Model");
            using (var sw = new StreamWriter(Path.Combine(modelDir, "Models.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.AspNetMvc.MvcModelsTemplate(config, allAggregates).TransformText());
            }

            var viewDir = Path.Combine(config.OutProjectDir, config.MvcViewDirectoryRelativePath);
            if (Directory.Exists(viewDir)) Directory.Delete(viewDir, recursive: true);
            Directory.CreateDirectory(viewDir);

            log?.WriteLine("コード自動生成: MVC View - MultiView");
            foreach (var rootAggregate in _rootAggregates) {
                var view = new CodeRendering.AspNetMvc.MvcMultiViewTemplate(config, rootAggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC View - SingleView");
            foreach (var rootAggregate in _rootAggregates) {
                var view = new CodeRendering.AspNetMvc.MvcSingleViewTemplate(config, rootAggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC View - CreateView");
            foreach (var rootAggregate in _rootAggregates) {
                var view = new CodeRendering.AspNetMvc.MvcCreateViewTemplate(config, rootAggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC View - 集約部分ビュー");
            foreach (var aggregate in allAggregates) {
                var view = new CodeRendering.AspNetMvc.InstancePartialViewTemplate(config, aggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成: MVC Controller");
            var controllerDir = Path.Combine(config.OutProjectDir, config.MvcControllerDirectoryRelativePath);
            if (Directory.Exists(controllerDir)) Directory.Delete(controllerDir, recursive: true);
            Directory.CreateDirectory(controllerDir);
            using (var sw = new StreamWriter(Path.Combine(controllerDir, "Controllers.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(new CodeRendering.AspNetMvc.MvcControllerTemplate(config, _rootAggregates).TransformText());
            }

            log?.WriteLine("コード自動生成: JS");
            {
                var view = new CodeRendering.AspNetMvc.JsTemplate();
                var filename = Path.Combine(viewDir, CodeRendering.AspNetMvc.JsTemplate.FILE_NAME);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成終了");
        }
        #endregion CodeGenerating


        #region Runtime

        public class RuntimeService : Runtime.IInstanceConvertingContext {
            internal RuntimeService(IServiceProvider serviceProvider, Assembly runtimeAssembly, RootAggregate[] rootAggregates) {
                _service = serviceProvider;
                _runtimeAssembly = runtimeAssembly;
                _rootAggregates = rootAggregates;
            }

            public string ApplicationName => "サンプルアプリケーション";

            private readonly IServiceProvider _service;
            private readonly Assembly _runtimeAssembly;
            private readonly RootAggregate[] _rootAggregates;

            private Config GetConfig() {
                return _service.GetRequiredService<Config>();
            }
            private IEnumerable<Aggregate> GetAllAggregates() {
                foreach (var root in _rootAggregates) {
                    yield return root;
                    foreach (var descendant in root.GetDescendants()) {
                        yield return descendant;
                    }
                }
            }
            private Microsoft.EntityFrameworkCore.DbContext GetDbContext() {
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
                    if (aggregateGuid == a.GUID) return true;
                    return false;
                });
            }

            public IEnumerable<Runtime.MenuItem> GetRootNavigations() {
                return _rootAggregates.Select(aggregate => new Runtime.MenuItem {
                    LinkText = aggregate.Name,
                    AspController = aggregate.Name,
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
                    errors = new[] { ex.Message };
                    instanceKey = string.Empty;
                    return false;
                } catch (DbUpdateException ex) {
                    errors = new[] { ex.Message };
                    instanceKey = string.Empty;
                    return false;
                }

                instanceKey = aggregate.CreateInstanceKeyFromUiInstnace(uiInstance).StringValue;
                errors = Array.Empty<string>();
                return true;
            }

            public TUIInstance? FindInstance<TUIInstance>(string instanceKey, out string instanceName) {
                var key = Runtime.InstanceKey.FromSerializedString(instanceKey);

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

            public IEnumerable<Runtime.AspNetMvc.AutoCompleteSource> LoadAutoCompleteDataSource(Guid aggregateGuid, string term) {
                var aggregate = FindAggregate(aggregateGuid);
                if (aggregate == null) throw new ArgumentException($"ID {aggregateGuid} と対応する集約が見つかりません。");

                var dbContext = GetDbContext();
                var method = aggregate.GetAutoCompleteMethod(_runtimeAssembly, dbContext);
                var result = (IEnumerable)method.Invoke(dbContext, new object[] { term })!;

                foreach (var item in result) {
                    yield return new AutoCompleteSource {
                        InstanceKey = aggregate.CreateInstanceKeyFromAutoCompleteItem(item).StringValue,
                        InstanceName = Runtime.InstanceName.Create(item, aggregate).Value,
                    };
                }
            }
        }
        #endregion Runtime
    }
}

