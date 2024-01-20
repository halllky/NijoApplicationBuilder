using Nijo.Core;
using Nijo.Features.Debugging;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Nijo.Util.CodeGenerating {
    public sealed class CodeRenderingContext {
        internal CodeRenderingContext() { }

        internal NijoCodeGenerator.DirectorySetupper? WebapiDir { get; set; }
        internal NijoCodeGenerator.DirectorySetupper? ReactDir { get; set; }

        public required Config Config { get; init; }
        public required AppSchema Schema { get; init; }

        public List<Func<string, string>> ConfigureServices { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureServicesWhenWebServer { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureServicesWhenBatchProcess { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureWebApp { get; } = new List<Func<string, string>>();

        public List<IReactPage> ReactPages { get; } = new List<IReactPage>();

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

        internal readonly Dictionary<GraphNode<Aggregate>, AggregateFile> _itemsByAggregate = new();
        public void Aggregate(GraphNode<Aggregate> aggregate, Action<AggregateFile> fn) {
            if (!_itemsByAggregate.TryGetValue(aggregate, out var item)) {
                item = new AggregateFile(aggregate);
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
        private void GenerateCode() {
            EditWebApiDirectory(genDir => {
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
                    efDir.Generate(new DbContextClass(Config).RenderDeclaring(this));
                });

                foreach (var aggFile in _itemsByAggregate.Values) {
                    genDir.Generate(aggFile.Render(this));
                }
            });

            EditReactDirectory(reactDir => {
                var reactProjectTemplate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ApplicationTemplates", "REACT_AND_WEBAPI", "react");

                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "index.tsx"));
                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "nijo-default-style.css"));
                reactDir.Generate(TypesTsx.Render(this));
                reactDir.Generate(MenuTsx.Render(this));

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
        #endregion アプリケーション基盤レンダリング
    }
}
