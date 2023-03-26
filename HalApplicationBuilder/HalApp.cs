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
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder {

    /// <summary>
    /// C#ソースコードの自動生成機能を提供します。
    /// </summary>
    public sealed class CodeGenerator {
        /// <summary>
        /// アセンブリ内から集約定義クラスを収集し、コード生成機能をもったオブジェクトを返します。
        /// </summary>
        /// <param name="assembly">このアセンブリ内から集約定義のクラスを収集します。</param>
        /// <param name="namespace">この名前空間の中にあるクラスのみを対象とします。未指定の場合、アセンブリ内の全クラスが対象となります。</param>
        /// <returns>コード生成機能をもったオブジェクト</returns>
        public static CodeGenerator FromAssembly(Assembly assembly, string? @namespace = null) {
            return new CodeGenerator(config => {
                var types = assembly
                    .GetTypes()
                    .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null);
                if (!string.IsNullOrWhiteSpace(@namespace)) {
                    types = types.Where(type => type.Namespace?.StartsWith(@namespace) == true);
                }
                return types.Select(t => new RootAggregate(config, new Core.Definition.ReflectionDefine(config, t, types)));
            });
        }
        /// <summary>
        /// 引数の型を集約定義として、コード生成機能をもったオブジェクトを返します。
        /// </summary>
        /// <param name="rootAggregateTypes">集約ルートの型</param>
        /// <returns>コード生成機能をもったオブジェクト</returns>
        public static CodeGenerator FromReflection(IEnumerable<Type> rootAggregateTypes) {
            return new CodeGenerator(config => {
                return rootAggregateTypes.Select(t => {
                    var def = new Core.Definition.ReflectionDefine(config, t, rootAggregateTypes);
                    return new RootAggregate(config, def);
                });
            });
        }

        internal CodeGenerator(Func<Config, IEnumerable<RootAggregate>> func) {
            _rootAggregateBuilder = func;
        }
        private readonly Func<Config, IEnumerable<RootAggregate>> _rootAggregateBuilder;

        /// <summary>
        /// コードの自動生成を実行します。
        /// </summary>
        /// <param name="config">コード生成に関する設定</param>
        /// <param name="log">このオブジェクトを指定した場合、コード生成の詳細を記録します。</param>
        public void GenerateCode(Config config, TextWriter? log = null) {

            log?.WriteLine($"コード自動生成開始");

            var _rootAggregates = _rootAggregateBuilder.Invoke(config);
            var allAggregates = _rootAggregates
                .SelectMany(a => a.GetDescendantsAndSelf())
                .ToArray();

            log?.WriteLine("コード自動生成: スキーマ定義");
            using (var sw = new StreamWriter(Path.Combine(config.OutProjectDir, "halapp.json"), append: false, encoding: Encoding.UTF8)) {
                var schema = new Serialized.AppSchemaJson {
                    Config = config.ToJson(onlyRuntimeConfig: true),
                    Aggregates = _rootAggregates.Select(a => a.ToJson()).ToArray(),
                };
                sw.Write(System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), // 日本語用
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, // nullのフィールドをシリアライズしない
                }));
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
    }


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
                return Core.Config.FromJson(schema.Config);
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
}
