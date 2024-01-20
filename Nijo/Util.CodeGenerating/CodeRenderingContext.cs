using Nijo.Core;
using Nijo.Parts;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Core.EnumDefinition;

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
        private readonly List<Action<Infrastructure>> _infraActions = new();
        private readonly Dictionary<AggregateMultiFeatureSourceKeys, List<Action<object>>> _aggregateMultiFeatureFiles = new();

        public void Render<T>(Action<T> fn) where T : Infrastructure {
            _infraActions.Add(infra => fn((T)infra));
        }
        private void GenerateMultiFeatureSources() {
            var infra = new Infrastructure();
            foreach (var action in _infraActions) action.Invoke(infra);
            infra.GenerateCode(this);

            foreach (var item in _aggregateMultiFeatureFiles) {
                var instance = (NijoFeatureBaseByAggregate)Activator.CreateInstance(item.Key._sourceType)!;
                foreach (var action in item.Value) {
                    action.Invoke(instance);
                }
                instance.GenerateCode(this, item.Key._aggregate);
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
    }
}
