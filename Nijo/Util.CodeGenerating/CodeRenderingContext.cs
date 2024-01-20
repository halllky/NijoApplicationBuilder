using Nijo.Core;
using Nijo.Features.Debugging;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;
using Nijo.Util.DotnetEx;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Nijo.Util.CodeGenerating {
    public class CodeRenderingContext {
        public required Config Config { get; init; }
        public required AppSchema Schema { get; init; }
        internal NijoCodeGenerator.DirectorySetupper? WebapiDir { get; set; }
        internal NijoCodeGenerator.DirectorySetupper? ReactDir { get; set; }
        public void EditWebApiDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler) => webapiDirHandler.Invoke(WebapiDir!);
        public void EditReactDirectory(Action<NijoCodeGenerator.DirectorySetupper> reactDirHandler) => reactDirHandler.Invoke(ReactDir!);

        internal void OnEndContext() {
            GenerateMultiFeatureSources();
            CleanUnhandledFilesAndDirectories();
        }


        #region 複数機能が1つのファイルにレンダリングされるもの
        internal struct AggregateMultiFeatureSourceKeys {
            internal GraphNode<Aggregate> _aggregate;
            internal Type _sourceType;
        }
        private readonly Dictionary<AggregateMultiFeatureSourceKeys, List<Action<object>>> _aggregateMultiFeatureFiles = new();

        private void GenerateMultiFeatureSources() {
            GenerateCode();

            foreach (var item in _aggregateMultiFeatureFiles) {
                var instance = (NijoFeatureBaseByAggregate)Activator.CreateInstance(item.Key._sourceType)!;
                foreach (var action in item.Value) {
                    action.Invoke(instance);
                }
                instance.GenerateCode(this, item.Key._aggregate);
            }
        }

        internal readonly Dictionary<GraphNode<Aggregate>, ByAggregate> _itemsByAggregate = new();
        public sealed class ByAggregate {
            // DbContext
            public bool HasDbSet { get; set; }
            public List<Func<string, string>> OnModelCreating { get; } = new();

            // AggregateRenderer
            public List<string> ControllerActions { get; } = new();
            public List<string> AppServiceMethods { get; } = new();
            public List<string> DataClassDeclaring { get; } = new();

            // react
            public List<string> TypeScriptDataTypes { get; } = new List<string>();
        }
        public void Aggregate(GraphNode<Aggregate> aggregate, Action<ByAggregate> fn) {
            if (!_itemsByAggregate.TryGetValue(aggregate, out var item)) {
                item = new ByAggregate();
                _itemsByAggregate.Add(aggregate, item);
            }
            fn(item);
        }
        #endregion 複数機能が1つのファイルにレンダリングされるもの


        #region 生成されなかったファイルの削除
        private readonly HashSet<string> _handled = new();
        internal void Handle(string fullpath) => _handled.Add(Path.GetFullPath(fullpath));
        private void CleanUnhandledFilesAndDirectories() {
            var allFiles
                = Directory.GetFiles(WebapiDir!.Path, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(ReactDir!.Path, "*", SearchOption.AllDirectories));
            foreach (var file in allFiles) {
                if (_handled.Contains(Path.GetFullPath(file))) continue;
                if (!File.Exists(file)) continue;
                File.Delete(file);
            }
            var allDirectories
                = Directory.GetDirectories(WebapiDir!.Path, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories(ReactDir!.Path, "*", SearchOption.AllDirectories));
            foreach (var dir in allDirectories) {
                if (_handled.Contains(Path.GetFullPath(dir))) continue;
                if (!Directory.Exists(dir)) continue;
                Directory.Delete(dir, true);
            }
        }
        #endregion 生成されなかったファイルの削除


        #region アプリケーション基盤レンダリング

        // DefaultConfigure
        public List<Func<string, string>> ConfigureServices { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureServicesWhenWebServer { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureServicesWhenBatchProcess { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureWebApp { get; } = new List<Func<string, string>>();

        // react
        public List<IReactPage> ReactPages { get; } = new List<IReactPage>();

        public void GenerateCode() {
            this.EditWebApiDirectory(genDir => {
                genDir.Generate(Configure.Render(this));
                genDir.Generate(EnumDefs.Render(this));
                genDir.Generate(new ApplicationService().Render(this));

                genDir.Directory("Util", utilDir => {
                    utilDir.Generate(RuntimeSettings.Render(this));
                    utilDir.Generate(Parts.Utility.DotnetExtensions.Render(this));
                    utilDir.Generate(Parts.Utility.AggregateUpdateEvent.Render(this));
                    utilDir.Generate(Parts.Utility.FromTo.Render(this));
                    utilDir.Generate(Parts.Utility.UtilityClass.RenderJsonConversionMethods(this));
                    utilDir.Generate(Features.Logging.HttpResponseExceptionFilter.Render(this));
                    utilDir.Generate(Features.Logging.DefaultLogger.Render(this));
                });
                genDir.Directory("Web", controllerDir => {
                    controllerDir.Generate(MultiView.RenderCSharpSearchConditionBaseClass(this));
                    controllerDir.Generate(DebuggerController.Render(this));
                });
                genDir.Directory("EntityFramework", efDir => {
                    efDir.Generate(new DbContextClass(this.Config).RenderDeclaring(this));
                });
            });

            foreach (var item in _itemsByAggregate) {
                RenderWebapiAggregateFile(this, item.Key, item.Value);
            }

            this.EditReactDirectory(reactDir => {
                var reactProjectTemplate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ApplicationTemplates", "REACT_AND_WEBAPI", "react");

                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "index.tsx"));
                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "nijo-default-style.css"));
                reactDir.Generate(new SourceFile {
                    FileName = "autogenerated-types.ts",
                    RenderContent = () => $$"""
                        import { UUID } from 'uuidjs'

                        {{_itemsByAggregate.SelectTextTemplate(item => $$"""
                        // ------------------ {{item.Key.Item.DisplayName}} ------------------
                        {{item.Value.TypeScriptDataTypes.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}

                        """)}}
                        """,
                });
                reactDir.Generate(new SourceFile {
                    FileName = "autogenerated-menu.tsx",
                    RenderContent = () => $$"""
                        {{ReactPages.SelectTextTemplate(page => $$"""
                        import {{page.ComponentPhysicalName}} from './{{REACT_PAGE_DIR}}/{{page.DirNameInPageDir}}/{{Path.GetFileNameWithoutExtension(page.GetSourceFile().FileName)}}'
                        """)}}

                        export const THIS_APPLICATION_NAME = '{{this.Schema.ApplicationName}}' as const

                        export const routes: { url: string, el: JSX.Element }[] = [
                        {{ReactPages.SelectTextTemplate(page => $$"""
                          { url: '{{page.Url}}', el: <{{page.ComponentPhysicalName}} /> },
                        """)}}
                        ]
                        export const menuItems: { url: string, text: string }[] = [
                        {{ReactPages.Where(p => p.ShowMenu).SelectTextTemplate(page => $$"""
                          { url: '{{page.Url}}', text: '{{page.LabelInMenu}}' },
                        """)}}
                        ]
                        """,
                });

                reactDir.Directory("collection", layoutDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "collection");
                    foreach (var file in Directory.GetFiles(source)) layoutDir.CopyFrom(file);
                });
                reactDir.Directory("input", userInputDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "input");
                    foreach (var file in Directory.GetFiles(source)) userInputDir.CopyFrom(file);

                    // TODO: どの集約がコンボボックスを作るのかをNijoFeatureBaseに主導権握らせたい
                    userInputDir.Generate(ComboBox.RenderDeclaringFile(this));
                });
                reactDir.Directory("util", reactUtilDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "util");
                    foreach (var file in Directory.GetFiles(source)) reactUtilDir.CopyFrom(file);
                    reactUtilDir.Generate(DummyDataGenerator.Render(this));
                });
                reactDir.Directory(REACT_PAGE_DIR, pageDir => {
                    foreach (var group in ReactPages.GroupBy(p => p.DirNameInPageDir)) {
                        pageDir.Directory(group.Key, aggregatePageDir => {
                            foreach (var page in group) {
                                aggregatePageDir.Generate(page.GetSourceFile());
                            }
                        });
                    }
                });
            });
        }

        internal const string REACT_PAGE_DIR = "pages";

        public interface IReactPage {
            string Url { get; }
            string DirNameInPageDir { get; }
            string ComponentPhysicalName { get; }
            bool ShowMenu { get; }
            string? LabelInMenu { get; }
            SourceFile GetSourceFile();
        }

        private void RenderWebapiAggregateFile(CodeRenderingContext context, GraphNode<Aggregate> aggregate, ByAggregate byAggregate) {

            context.EditWebApiDirectory(dir => {
                var appSrv = new ApplicationService();
                var controller = new Parts.WebClient.Controller(aggregate.Item);

                dir.Generate(new SourceFile {
                    FileName = $"{aggregate.Item.DisplayName.ToFileNameSafe()}.cs",
                    RenderContent = () => $$"""
                        namespace {{context.Config.RootNamespace}} {
                            using System;
                            using System.Collections;
                            using System.Collections.Generic;
                            using System.ComponentModel;
                            using System.ComponentModel.DataAnnotations;
                            using System.Linq;
                            using Microsoft.AspNetCore.Mvc;
                            using Microsoft.EntityFrameworkCore;
                            using Microsoft.EntityFrameworkCore.Infrastructure;
                            using {{context.Config.EntityNamespace}};

                            [ApiController]
                            [Route("{{Parts.WebClient.Controller.SUBDOMAIN}}/[controller]")]
                            public partial class {{controller.ClassName}} : ControllerBase {
                                public {{controller.ClassName}}(ILogger<{{controller.ClassName}}> logger, {{appSrv.ClassName}} applicationService) {
                                    _logger = logger;
                                    _applicationService = applicationService;
                                }
                                protected readonly ILogger<{{controller.ClassName}}> _logger;
                                protected readonly {{appSrv.ClassName}} _applicationService;

                                {{WithIndent(byAggregate.ControllerActions, "        ")}}
                            }


                            partial class {{appSrv.ClassName}} {
                                {{WithIndent(byAggregate.AppServiceMethods, "        ")}}
                            }


                        #region データ構造クラス
                            {{WithIndent(byAggregate.DataClassDeclaring, "    ")}}
                        #endregion データ構造クラス
                        }

                        namespace {{context.Config.DbContextNamespace}} {
                            using {{context.Config.RootNamespace}};
                            using Microsoft.EntityFrameworkCore;

                            partial class {{context.Config.DbContextName}} {
                        {{If(byAggregate.HasDbSet, () => aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""
                                public virtual DbSet<{{agg.Item.EFCoreEntityClassName}}> {{agg.Item.DbSetName}} { get; set; }
                        """))}}

                        {{If(byAggregate.OnModelCreating.Any(), () => $$"""
                                private void OnModelCreating_{{aggregate.Item.ClassName}}(ModelBuilder modelBuilder) {
                                    {{WithIndent(byAggregate.OnModelCreating.SelectTextTemplate(fn => fn.Invoke("modelBuilder")), "            ")}}
                                }
                        """)}}
                            }
                        }
                        """,
                });
            });
        }
        #endregion アプリケーション基盤レンダリング
    }
}
