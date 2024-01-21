using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts.Utility;

namespace Nijo.Features.BatchUpdate {
    internal class BatchUpdateTask {

        internal static SourceFile Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var availableAggregates = context.Schema
                .RootAggregates()
                .Where(a => a.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);

            return new SourceFile {
                FileName = "BatchUpdateTask.cs",
                RenderContent = () => $$"""
                    namespace {{context.Config.RootNamespace}} {
                        using System.Text.Json;

                        public class BatchUpdateParameter {
                            public string? DataType { get; set; }
                            public List<BatchUpdateData>? Items { get; set; } = new();
                        }
                        public class BatchUpdateData {
                            public E_BatchUpdateAction? Action { get; set; }
                            public object? Data { get; set; }
                        }
                        public enum E_BatchUpdateAction {
                            Add,
                            Modify,
                            Delete,
                        }

                        public class BatchUpdateTask : BackgroundTask<BatchUpdateParameter> {
                            public override string BatchTypeId => "NIJO::BATCH-UPDATE";

                            public override string GetJobName(BatchUpdateParameter param) {
                                return $"一括アップデート（{param.DataType}）";
                            }

                            public override IEnumerable<string> ValidateParameter(BatchUpdateParameter parameter) {
                    {{availableAggregates.Select(agg => GetKey(agg)).SelectTextTemplate(key => $$"""
                                if (parameter.DataType == "{{key}}") yield break;
                    """)}}
                                yield return $"識別子 '{parameter.DataType}' と対応する一括更新処理はありません。";
                            }

                            public override void Execute(JobChainWithParameter<BatchUpdateParameter> job) {
                                job.Section("更新処理実行", context => {
                                    switch (context.Parameter.DataType) {
                    {{availableAggregates.SelectTextTemplate(agg => $$"""
                                        case "{{GetKey(agg)}}": {{UpdateMethodName(agg)}}(context); break;
                    """)}}
                                        default: throw new InvalidOperationException($"識別子 '{context.Parameter.DataType}' と対応する一括更新処理はありません。");
                                    }
                                });
                            }
                    {{availableAggregates.SelectTextTemplate(agg => $$"""

                            {{WithIndent(RenderUpdateMethod(agg, context), "        ")}}
                    """)}}
                        }
                    }
                    """,
            };
        }

        private static string RenderUpdateMethod(GraphNode<Aggregate> agg, CodeRenderingContext context) {
            var appSrv = new ApplicationService().ClassName;
            var create = new Models.WriteModel.CreateFeature(agg);
            var update = new Models.WriteModel.UpdateFeature(agg);
            var delete = new Models.WriteModel.DeleteFeature(agg);
            var delKeys = KeyArray.Create(agg, nullable: false);

            return $$"""
                private void {{UpdateMethodName(agg)}}(BackgroundTaskContext<BatchUpdateParameter> context) {
                    if (context.Parameter.Items == null || context.Parameter.Items.Count == 0) {
                        context.Logger.LogWarning("パラメータが０件です。");
                        return;
                    }
                    for (int i = 0; i < context.Parameter.Items.Count; i++) {
                        using var logScope = context.Logger.BeginScope($"{i + 1}件目");
                        try {
                            var item = context.Parameter.Items[i];
                            if (item.Action == null) throw new InvalidOperationException("登録・更新・削除のいずれかを指定してください。");
                            if (item.Data == null) throw new InvalidOperationException("データが空です。");

                            using var serviceScope = context.ServiceProvider.CreateScope();
                            var scopedAppSrv = serviceScope.ServiceProvider.GetRequiredService<{{appSrv}}>();

                            ICollection<string> errors;
                            switch (item.Action) {
                                case E_BatchUpdateAction.Add:
                                    var cmd = {{UtilityClass.CLASSNAME}}.{{UtilityClass.ENSURE_OBJECT_TYPE}}<{{create.ArgType}}>(item.Data)
                                        ?? throw new InvalidOperationException($"パラメータを{nameof({{create.ArgType}})}型に変換できません。");
                                    if (!scopedAppSrv.{{create.MethodName}}(cmd, out var _, out errors))
                                        throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                                    break;
                                case E_BatchUpdateAction.Modify:
                                    var data = {{UtilityClass.CLASSNAME}}.{{UtilityClass.ENSURE_OBJECT_TYPE}}<{{update.ArgType}}>(item.Data)
                                        ?? throw new InvalidOperationException($"パラメータを{nameof({{update.ArgType}})}型に変換できません。");
                                    if (!scopedAppSrv.{{update.MethodName}}(data, out var _, out errors))
                                        throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                                    break;
                                case E_BatchUpdateAction.Delete:
                                    var key = item.Data as object[]
                                        ?? throw new InvalidOperationException($"パラメータを削除対象データのキーの配列に変換できません。");
                                    if (!scopedAppSrv.{{delete.MethodName}}({{delKeys.Select((k, i) => $"({k.CsType})key[{i}]").Join(", ")}}, out errors))
                                        throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                                    break;
                                default:
                                    throw new InvalidOperationException($"認識できない更新処理種別です: {item.Action}");
                            }
                        } catch (Exception ex) {
                            context.Logger.LogError(ex, "更新処理に失敗しました。");
                            continue;
                        }
                        context.Logger.LogInformation("正常終了");
                    }
                }
                """;
        }

        private static string GetKey(GraphNode<Aggregate> aggregate) {
            return aggregate.Item.ClassName;
        }
        private static string UpdateMethodName(GraphNode<Aggregate> aggregate) {
            return "BatchUpdate" + aggregate.Item.ClassName;
        }
    }
}
