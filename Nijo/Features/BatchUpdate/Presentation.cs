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

        #region WebAPIでリアルタイムで実行
        internal static Controller GetBatchUpdateController() => new Controller("BatchUpdate");

        private static SourceFile RenderController() {
            return new SourceFile {
                FileName = "BatchUpdateController.cs",
                RenderContent = context => {
                    var appSrv = new ApplicationService();
                    var controller = GetBatchUpdateController();

                    return $$"""
                        namespace {{context.Config.RootNamespace}} {
                            using Microsoft.AspNetCore.Mvc;

                            [ApiController]
                            [Route("{{Controller.SUBDOMAIN}}/[controller]")]
                            public partial class {{controller.ClassName}} : ControllerBase {
                                public {{controller.ClassName}}({{appSrv.ClassName}} applicationService) {
                                    _applicationService = applicationService;
                                }

                                private readonly {{appSrv.ClassName}} _applicationService;

                                [HttpPost("")]
                                public virtual IActionResult Index([FromBody] BatchUpdateFeature.Parameter parameter) {
                                    if (!BatchUpdateFeature.TryCreate(parameter, out var command, out var errors)) {
                                        return BadRequest($"パラメータが不正です。{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
                                    }

                                    using var tran = _applicationService.DbContext.Database.BeginTransaction();
                                    try {
                                        if (!command.Execute(_applicationService, out var errors2)) {
                                            tran.Rollback();
                                            return Problem($"一括更新に失敗しました。{Environment.NewLine}{string.Join(Environment.NewLine, errors2)}");
                                        }
                                        tran.Commit();
                                        return Ok();

                                    } catch (Exception ex) {
                                        tran.Rollback();
                                        return Problem(ex.ToString());
                                    }
                                }
                            }
                        }
                        """;
                },
            };
        }
        #endregion WebAPIでリアルタイムで実行


        #region スケジューリングして非同期で実行
        private const string JOBKEY = "NIJO-BATCH-UPDATE";

        private const string PARAM_ITEMS = "Items";
        private const string PARAM_DATATYPE = "DataType";
        private const string PARAM_ACTION = "Action";
        private const string PARAM_DATA = "Data";

        private const string ACTION_ADD = "ADD";
        private const string ACTION_MODIFY = "MOD";
        private const string ACTION_DELETE = "DEL";

        private static SourceFile RenderTaskDefinition() {
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
        #endregion スケジューリングして非同期で実行
    }
}
