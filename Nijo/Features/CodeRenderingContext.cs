using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Features.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    public interface ICodeRenderingContext {
        Config Config { get; }
        AppSchema Schema { get; }

        void AddQuery(NodeId aggregateId);
        void AddCommand(NodeId aggregateId);
        IWebApiProject WebApiProject { get; }
        IReactProject ReactProject { get; }

        public interface IWebApiProject {
            void RenderServiceProvider(Func<string, string> sourceCode);
            void RenderControllerAction(Func<WebClient.Controller, string> sourceCode);
            void RenderApplicationServiceMethod(Func<ApplicationService, string> sourceCode);
            void RenderDataClassDeclaring(string sourceCode);
            void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler);
        }
        public interface IReactProject {
            void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> reactDirHandler);
            void RenderDataClassDeclaring(string sourceCode);
        }
    }
    internal class CodeRenderingContext : ICodeRenderingContext {
        public required Config Config { get; init; }
        public required AppSchema Schema { get; init; }
        public required ICodeRenderingContext.IWebApiProject WebApiProject { get; set; }
        public required ICodeRenderingContext.IReactProject ReactProject { get; set; }

        public void AddCommand(NodeId aggregateId) {
            throw new NotImplementedException();
        }
        public void AddQuery(NodeId aggregateId) {
            throw new NotImplementedException();
        }

        #region 複数機能にまたがるソース
        internal struct MultiSourceKeys {
            internal NijoCodeGenerator.DirectorySetupper _directory;
            internal GraphNode<Aggregate> _aggregate;
            internal Type _sourceType;
        }
        private readonly Dictionary<MultiSourceKeys, List<string>> _multiFeatureFiles = new();
        internal void Insert<T>(MultiSourceKeys key, string sourceCode) where T : ISourceFileUsedByMultiFeature, new() {
            if (_multiFeatureFiles.TryGetValue(key, out var list)) {
                list.Add(sourceCode);
            } else {
                _multiFeatureFiles.Add(key, new List<string> { sourceCode });
            }
        }
        internal void GenerateMultiFeatureSourceFiles() {
            foreach (var item in _multiFeatureFiles) {
                var instance = (ISourceFileUsedByMultiFeature)Activator.CreateInstance(item.Key._sourceType)!;
                var sourceFile = instance.GenerateSourceFile(item.Key._aggregate, item.Value);
                item.Key._directory.Generate(sourceFile);
            }
        }
        #endregion 複数機能にまたがるソース


        internal class WebApi : ICodeRenderingContext.IWebApiProject {
            internal NijoCodeGenerator.DirectorySetupper? DirectorySetupper { get; set; }

            internal GraphNode<Aggregate>? CurrentAggregate { get; set; }

            internal StringBuilder DataClassSource { get; } = new StringBuilder();
            internal StringBuilder ServiceProviderSource { get; } = new StringBuilder();

            public void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> webapiDirHandler) {
                webapiDirHandler(DirectorySetupper!);
            }
            public void RenderApplicationServiceMethod(Func<ApplicationService, string> sourceCode) {

            }
            public void RenderControllerAction(Func<Controller, string> sourceCode) {

            }
            public void RenderDataClassDeclaring(string sourceCode) {
                DataClassSource.AppendLine(sourceCode);
            }
            public void RenderServiceProvider(Func<string, string> sourceCode) {
                ServiceProviderSource.AppendLine(sourceCode("serviceProvider"));
            }
        }
        internal class React : ICodeRenderingContext.IReactProject {
            internal NijoCodeGenerator.DirectorySetupper? DirectorySetupper { get; set; }

            public void EditDirectory(Action<NijoCodeGenerator.DirectorySetupper> reactDirHandler) {
                reactDirHandler(DirectorySetupper!);
            }

            public void RenderDataClassDeclaring(string sourceCode) {

            }
        }
    }
}
