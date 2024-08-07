using Nijo.Parts.WebServer;
using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.Models;
using Nijo.Features;

namespace Nijo {
    /// <summary>
    /// コード自動生成機能
    /// </summary>
    public sealed class NijoCodeGenerator {

        /// <summary>
        /// 生成後のプロジェクトに具備される機能を列挙します。
        /// </summary>
        internal IEnumerable<IFeature> GetFeatures() {
            yield return new Features.Debugging.DebuggingFeature();
            yield return new Features.Logging.LoggingFeature();
            yield return new Features.BackgroundService.BgTaskFeature();
            if (!_project.ReadConfig().DisableBatchUpdate) {
                yield return new Features.BatchUpdate.BatchUpdateFeature();
            }
        }

        internal static class Models {
            internal static KeyValuePair<string, Func<IModel>> WriteModel => KeyValuePair.Create("write-model", () => (IModel)new WriteModel());
            internal static KeyValuePair<string, Func<IModel>> ReadModel => KeyValuePair.Create("read-model", () => (IModel)new ReadModel());

            internal static IEnumerable<KeyValuePair<string, Func<IModel>>> GetAll() {
                yield return WriteModel;
                yield return ReadModel;
            }
        }


        internal NijoCodeGenerator(GeneratedProject project, ILogger? log) {
            _project = project;
            _log = log;
        }

        private readonly GeneratedProject _project;
        private readonly ILogger? _log;

        /// <summary>
        /// コード生成の実行時オプション
        /// </summary>
        public sealed class CodeGenerateOptions {
            public bool OverwriteConcreteAppSrvFile { get; set; }
        }

        /// <summary>
        /// コードの自動生成を行います。
        /// </summary>
        public NijoCodeGenerator GenerateCode(CodeGenerateOptions? options = null) {

            _log?.LogInformation($"コード自動生成開始: {_project.SolutionRoot}");

            // コンテキスト生成
            var ctx = new CodeRenderingContext(_project, options ?? new());

            // 初期化
            _project.CoreLibrary.CreateProjectIfNotExists(ctx.Config);
            _project.WebApiProject.CreateProjectIfNotExists(ctx.Config);
            _project.ReactProject.CreateProjectIfNotExists(ctx.Config);
            _project.CliProject.CreateProjectIfNotExists(ctx.Config);

            var features = GetFeatures().ToArray();
            var nonAggregateFeatures = features
                .OfType<IFeature>();
            foreach (var feature in nonAggregateFeatures) {
                feature.GenerateCode(ctx);
            }

            var handledModels = new HashSet<string>();
            var handlers = Models
                .GetAll()
                .ToDictionary(kv => kv.Key, kv => kv.Value.Invoke());
            foreach (var rootAggregate in ctx.Schema.RootAggregates()) {
                if (!string.IsNullOrWhiteSpace(rootAggregate.Item.Options.Handler)
                    && handlers.TryGetValue(rootAggregate.Item.Options.Handler, out var model)) {
                    model.GenerateCode(ctx, rootAggregate);
                    handledModels.Add(rootAggregate.Item.Options.Handler);
                } else {
                    // 特に指定の無い集約は処理対象外
                }
            }

            // 複数の集約から1個のソースが作成されるもの等はこのタイミングで作成
            ctx.OnEndContext(handledModels);

            // 自動生成されるソースコードをカスタマイズするクラスを呼び出す
            var customizers = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass
                            && !type.IsAbstract
                            && typeof(IAutoGeneratedCodeCustomizer).IsAssignableFrom(type));
            foreach (var type in customizers) {
                try {
                    var customizerInstance = (IAutoGeneratedCodeCustomizer?)Activator.CreateInstance(type)
                        ?? throw new InvalidOperationException($"{type.FullName} クラスのインスタンス化に失敗しました。");
                    customizerInstance.CustomizeAutoGeneratedCode(ctx);
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{type.FullName} クラスのインスタンス化に失敗しました。{nameof(IAutoGeneratedCodeCustomizer)}インターフェースを実装するクラスは引数なしのコンストラクタを持っている必要があります。", ex);
                }
            }

            _log?.LogInformation($"コード自動生成終了: {_project.SolutionRoot}");
            return this;
        }
    }
}
