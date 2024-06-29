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
        internal CodeRenderingContext(GeneratedProject app) {
            WebApiProject = new WebApiProject.DirectoryEditor(this, app.WebApiProject);
        }

        internal DirectorySetupper? _webapiDir;
        internal DirectorySetupper? _reactDir;

        public required Config Config { get; init; }
        public required AppSchema Schema { get; init; }
        public required NijoCodeGenerator.CodeGenerateOptions Options { get; init; }

        /// <summary>自動生成される ASP.NET Core API プロジェクト</summary>
        public WebApiProject.DirectoryEditor WebApiProject { get; }

        public void EditReactDirectory(Action<DirectorySetupper> reactDirHandler) => reactDirHandler.Invoke(_reactDir!);

        internal readonly App _app = new();

        // TODO: このへんのラッパーメソッドが冗長
        public void UseAggregateFile(GraphNode<Aggregate> aggregate, Action<AggregateFile> fn) => _app.Aggregate(aggregate, fn);
        public void ConfigureServices(Func<string, string> fn) => _app.ConfigureServices.Add(fn);
        public void ConfigureServicesWhenBatchProcess(Func<string, string> fn) => _app.ConfigureServicesWhenBatchProcess.Add(fn);
        public void AddPage(IReactPage page) => _app.ReactPages.Add(page);
        public void AddAppSrvMethod(string source) => _app.AppSrvMethods.Add(source);

        internal void OnEndContext() {
            _app.GenerateCode(this);
            CleanUnhandledFilesAndDirectories();
        }

        public Assembly ExecutingAssembly => _executingAssembly ??= Assembly.GetExecutingAssembly();
        private Assembly? _executingAssembly;

        internal EmbeddedResource.Collection EmbeddedResources => _embeddedResources ??= new EmbeddedResource.Collection(ExecutingAssembly);
        private EmbeddedResource.Collection? _embeddedResources;

        #region 生成されなかったファイルの削除
        private readonly HashSet<string> _handled = new();
        internal void Handle(string fullpath) => _handled.Add(Path.GetFullPath(fullpath));
        internal bool IsHandled(string fullpath) => _handled.Contains(Path.GetFullPath(fullpath));
        private void CleanUnhandledFilesAndDirectories() {
            var allFiles
                = Directory.GetFiles(_webapiDir!.Path, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_reactDir!.Path, "*", SearchOption.AllDirectories));
            foreach (var file in allFiles) {
                if (IsHandled(file)) continue;
                if (!File.Exists(file)) continue;
                File.Delete(file);
            }
            var allDirectories
                = Directory.GetDirectories(_webapiDir!.Path, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories(_reactDir!.Path, "*", SearchOption.AllDirectories));
            foreach (var dir in allDirectories) {
                if (IsHandled(dir)) continue;
                if (!Directory.Exists(dir)) continue;
                Directory.Delete(dir, true);
            }
        }
        #endregion 生成されなかったファイルの削除
    }
}
