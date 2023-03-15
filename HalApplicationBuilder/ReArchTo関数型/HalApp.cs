using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using HalApplicationBuilder.ReArchTo関数型.Core;

namespace HalApplicationBuilder.ReArchTo関数型
{
    public class HalApp {

        #region Configure
        public static void Configure(IServiceCollection serviceCollection, Config config, Type[] rootAggregateTypes) {
            serviceCollection.AddScoped(_ => config);
            serviceCollection.AddScoped(provider => {
                var aggregates = rootAggregateTypes
                    .Select(t => RootAggregate.FromReflection(t))
                    .ToArray();
                return new HalApp(provider, aggregates);
            });
        }
        public static void Configure(IServiceCollection serviceCollection, Config config, Assembly assembly, string? @namespace = null) {
            serviceCollection.AddScoped(_ => config);
            serviceCollection.AddScoped(provider => {
                var rootAggregateTypes = assembly
                    .GetTypes()
                    .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null);
                if (!string.IsNullOrWhiteSpace(@namespace)) {
                    rootAggregateTypes = rootAggregateTypes.Where(type => type.Namespace?.StartsWith(@namespace) == true);
                }
                var aggregates = rootAggregateTypes
                    .Select(t => RootAggregate.FromReflection(t))
                    .ToArray();
                return new HalApp(provider, aggregates);
            });
        }
        #endregion


        private HalApp(IServiceProvider serviceProvider, RootAggregate[] rootAggregates) {
            _services = serviceProvider;
            _rootAggregates = rootAggregates;
        }

        private readonly IServiceProvider _services;
        private readonly RootAggregate[] _rootAggregates;

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
                .Concat(_rootAggregates.SelectMany(a => a.GetDescendants()))
                .ToArray();

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
                var view = new AspNetMvc.JsTemplate();
                var filename = Path.Combine(viewDir, CodeRendering.AspNetMvc.JsTemplate.FILE_NAME);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成終了");
        }
    }
}

