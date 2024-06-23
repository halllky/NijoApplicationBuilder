using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts.Utility;

namespace Nijo.Features.BatchUpdate {
    partial class BatchUpdateFeature {
        private const string JOBKEY = "NIJO-BATCH-UPDATE";

        private const string PARAM_ITEMS = "Items";
        private const string PARAM_DATATYPE = "DataType";
        private const string PARAM_ACTION = "Action";
        private const string PARAM_DATA = "Data";

        private const string ACTION_ADD = "ADD";
        private const string ACTION_MODIFY = "MOD";
        private const string ACTION_DELETE = "DEL";

        private static SourceFile RenderTaskDefinition(CodeRenderingContext context) {
            var appSrv = new ApplicationService();

            return new SourceFile {
                FileName = "BatchUpdateTask.cs",
                RenderContent = context => $$"""
                    namespace {{context.Config.RootNamespace}} {
                        using System.Text.Json;

                        public class BatchUpdateTask : BackgroundTask<BatchUpdateFeature.Parameter> {
                            public override string BatchTypeId => "{{JOBKEY}}";

                            public override string GetJobName(BatchUpdateFeature.Parameter param) {
                                return $"一括アップデート（全{param.Items.Count}件）";
                            }

                            public override IEnumerable<string> ValidateParameter(BatchUpdateFeature.Parameter parameter) {
                                BatchUpdateFeature.TryCreate(parameter, out var _, out var errors);
                                foreach (var error in errors) {
                                    yield return error;
                                }
                            }

                            public override void Execute(JobChainWithParameter<BatchUpdateFeature.Parameter> job) {
                                job.Section("更新処理実行", context => {
                                    if (!BatchUpdateFeature.TryCreate(context.Parameter, out var command, out var errors)) {
                                        throw new InvalidOperationException($"パラメータが不正です。{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
                                    }

                                    using var tran = context.DbContext.Database.BeginTransaction();
                                    try {
                                        if (!command.Execute(context.AppSrv, out var errors2)) {
                                            throw new InvalidOperationException($"一括更新に失敗しました。{Environment.NewLine}{string.Join(Environment.NewLine, errors2)}");
                                        }
                                        tran.Commit();

                                    } catch {
                                        tran.Rollback();
                                        throw;
                                    }
                                });
                            }
                        }
                    }
                    """,
            };
        }
    }
}
