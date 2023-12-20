using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Features.WebClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    public interface ICodeRenderingContext {
        Config Config { get; }
        AppSchema Schema { get; }

        void EditWebApiDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler);
        void EditReactDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler);

        void Render<T>(Action<T> handler) where T : ISourceFileUsedByMultiFeature;
        void Render<T>(GraphNode<Aggregate> aggregate, Action<T> handler) where T : IAggregateSourceFileUsedByMultiFeature;

        //public interface IWebApiProject {
        //    //void RenderServiceProvider(Func<string, string> sourceCode);
        //    //void RenderControllerAction(Func<WebClient.Controller, string> sourceCode);
        //    //void RenderApplicationServiceMethod(Func<ApplicationService, string> sourceCode);
        //    //void RenderDataClassDeclaring(string sourceCode);
        //    void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler);
        //}
        //public interface IReactProject {
        //    void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> reactDirHandler);
        //    //void RenderDataClassDeclaring(string sourceCode);
        //}
    }
    internal class CodeRenderingContext : ICodeRenderingContext {
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
        private readonly Dictionary<Type, List<Action<object>>> _multiFeatureFiles = new();
        private readonly Dictionary<AggregateMultiFeatureSourceKeys, List<Action<object>>> _aggregateMultiFeatureFiles = new();

        public void Render<T>(Action<T> handler) where T : ISourceFileUsedByMultiFeature {
            var action = (object arg) => handler((T)arg);
            if (_multiFeatureFiles.TryGetValue(typeof(T), out var list)) {
                list.Add(action);
            } else {
                _multiFeatureFiles.Add(typeof(T), new List<Action<object>> { action });
            }
        }
        public void Render<T>(GraphNode<Aggregate> aggregate, Action<T> handler) where T : IAggregateSourceFileUsedByMultiFeature {
            var action = (object arg) => handler((T)arg);
            var key = new AggregateMultiFeatureSourceKeys {
                _aggregate = aggregate,
                _sourceType = typeof(T),
            };
            if (_aggregateMultiFeatureFiles.TryGetValue(key, out var list)) {
                list.Add(action);
            } else {
                _aggregateMultiFeatureFiles.Add(key, new List<Action<object>> { action });
            }
        }
        private void GenerateMultiFeatureSources() {
            foreach (var item in _multiFeatureFiles) {
                var instance = (ISourceFileUsedByMultiFeature)Activator.CreateInstance(item.Key)!;
                foreach (var action in item.Value) {
                    action.Invoke(instance);
                }
                instance.GenerateSourceFile(this);
            }
            foreach (var item in _aggregateMultiFeatureFiles) {
                var instance = (IAggregateSourceFileUsedByMultiFeature)Activator.CreateInstance(item.Key._sourceType)!;
                foreach (var action in item.Value) {
                    action.Invoke(instance);
                }
                instance.GenerateSourceFile(this, item.Key._aggregate);
            }
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

        //internal class WebApi : ICodeRenderingContext.IWebApiProject {
        //    internal NijoCodeGenerator.DirectorySetupper? DirectorySetupper { get; set; }

        //    internal GraphNode<Aggregate>? CurrentAggregate { get; set; }

        //    internal StringBuilder DataClassSource { get; } = new StringBuilder();
        //    internal StringBuilder ServiceProviderSource { get; } = new StringBuilder();

        //    public void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler) {
        //        webapiDirHandler(DirectorySetupper!);
        //    }
        //    public void RenderApplicationServiceMethod(Func<ApplicationService, string> sourceCode) {

        //    }
        //    public void RenderControllerAction(Func<Controller, string> sourceCode) {

        //    }
        //    public void RenderDataClassDeclaring(string sourceCode) {
        //        DataClassSource.AppendLine(sourceCode);
        //    }
        //    public void RenderServiceProvider(Func<string, string> sourceCode) {
        //        ServiceProviderSource.AppendLine(sourceCode("serviceProvider"));
        //    }
        //}
        //internal class React : ICodeRenderingContext.IReactProject {
        //    internal NijoCodeGenerator.DirectorySetupper? DirectorySetupper { get; set; }

        //    public void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> reactDirHandler) {
        //        reactDirHandler(DirectorySetupper!);
        //    }

        //    public void RenderDataClassDeclaring(string sourceCode) {

        //    }
        //}
    }
}
