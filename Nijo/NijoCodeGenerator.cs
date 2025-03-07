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
using Nijo.Util.DotnetEx;

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
        }

        internal static class Models {
            internal static KeyValuePair<string, Func<IModel>> WriteModel2 => KeyValuePair.Create("write-model-2", () => (IModel)new WriteModel2());
            internal static KeyValuePair<string, Func<IModel>> ReadModel2 => KeyValuePair.Create("read-model-2", () => (IModel)new ReadModel2());
            internal static KeyValuePair<string, Func<IModel>> CommandModel => KeyValuePair.Create("command", () => (IModel)new CommandModel());
            internal static KeyValuePair<string, Func<IModel>> ValueObjectModel => KeyValuePair.Create("value-object", () => (IModel)new ValueObjectModel());

            internal static IEnumerable<KeyValuePair<string, Func<IModel>>> GetAll() {
                yield return WriteModel2;
                yield return ReadModel2;
                yield return CommandModel;
                yield return ValueObjectModel;
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
            /// <summary>
            /// コード自動生成の最後に実行される処理
            /// </summary>
            public Action<CodeRenderingContext>? OnEndGenerating { get; set; }
        }

        /// <summary>
        /// コードの自動生成を行います。
        /// </summary>
        public NijoCodeGenerator GenerateCode(CodeGenerateOptions? options = null) {

            _log?.LogInformation("コード自動生成開始: {solutionRoot}", _project.SolutionRoot);

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

            var validationErrors = new Dictionary<GraphNode<Aggregate>, IEnumerable<string>>();
            foreach (var rootAggregate in ctx.Schema.RootAggregates()) {
                _log?.LogInformation("モデル生成開始: {rootAggregate} ({handler})",
                    rootAggregate.Item.DisplayName,
                    rootAggregate.Item.Options.Handler);

                if (!string.IsNullOrWhiteSpace(rootAggregate.Item.Options.Handler)) {
                    var model = ctx.GetModel(rootAggregate.Item.Options.Handler);
                    var modelErrors = model.ValidateAggregate(rootAggregate).ToArray();
                    if (modelErrors.Length > 0) {
                        validationErrors.Add(rootAggregate, modelErrors);
                        continue;
                    }
                    model.GenerateCode(ctx, rootAggregate);
                } else {
                    // 特に指定の無い集約は処理対象外
                }
            }
            if (validationErrors.Count > 0) {
                throw new InvalidOperationException($$"""
                    集約定義が不正です。
                    {{validationErrors.SelectTextTemplate(err => $$"""
                    - {{err.Key.Item.DisplayName}}
                    {{err.Value.SelectTextTemplate(msg => $$"""
                      - {{WithIndent(msg, "    ")}}
                    """)}}
                    """)}}

                    """);
            }

            // 複数の集約から1個のソースが作成されるもの等はこのタイミングで作成
            ctx.OnEndContext();

            // 自動テスト用の挿入コードなど
            options?.OnEndGenerating?.Invoke(ctx);

            _log?.LogInformation("コード自動生成終了: {solutionRoot}", _project.SolutionRoot);
            return this;
        }
    }
}
