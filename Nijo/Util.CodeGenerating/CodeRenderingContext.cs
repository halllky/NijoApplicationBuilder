using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly Dictionary<Type, List<Action<object>>> _multiFeatureFiles = new();
        private readonly Dictionary<AggregateMultiFeatureSourceKeys, List<Action<object>>> _aggregateMultiFeatureFiles = new();

        public void Render<T>(Action<T> handler) where T : NijoFeatureBaseNonAggregate {
            var action = (object arg) => handler((T)arg);
            if (_multiFeatureFiles.TryGetValue(typeof(T), out var list)) {
                list.Add(action);
            } else {
                _multiFeatureFiles.Add(typeof(T), new List<Action<object>> { action });
            }
        }
        public void Render<T>(GraphNode<Aggregate> aggregate, Action<T> handler) where T : NijoFeatureBaseByAggregate {
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
                var instance = (NijoFeatureBaseNonAggregate)Activator.CreateInstance(item.Key)!;
                foreach (var action in item.Value) {
                    action.Invoke(instance);
                }
                instance.GenerateCode(this);
            }
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
