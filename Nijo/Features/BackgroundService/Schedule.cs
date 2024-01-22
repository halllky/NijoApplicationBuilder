using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {

        private const string SCHEDULE = "schedule";

        private string RenderAspControllerAction(CodeRenderingContext ctx) {
            return $$"""
                [HttpPost("{{SCHEDULE}}/{jobType}")]
                public virtual IActionResult Schedule(string? jobType, [FromBody] object? param) {
                    if (string.IsNullOrWhiteSpace(jobType)) {
                        return BadRequest("ジョブ種別を指定してください。");

                    } else if (!_applicationService.TryScheduleJob(jobType, param, out var errors)) {
                        return BadRequest(string.Join(Environment.NewLine, errors));

                    } else {
                        return Ok();
                    }
                }
                """;
        }

        private string RenderAppSrvMethod(CodeRenderingContext ctx) {

            return $$"""
                #region 非同期処理
                public bool TryScheduleJob(string batchType, object? parameter, out ICollection<string> errors) {
                    BackgroundTask job;
                    try {
                        job = BackgroundTask.FindTaskByID(batchType);
                    } catch (Exception ex) {
                        errors = new[] { ex.Message };
                        return false;
                    }
                    return TryScheduleJob(job, parameter, out errors);
                }
                public bool TryScheduleJob<TJob>(out ICollection<string> errors)
                    where TJob : BackgroundTask, new() {
                    var job = new TJob();
                    return TryScheduleJob(job, null, out errors);
                }
                public bool TryScheduleJob<TJob, TParameter>(TParameter parameter, out ICollection<string> errors)
                    where TJob : BackgroundTask<TParameter>, new()
                    where TParameter : new() {
                    var job = new TJob();
                    return TryScheduleJob(job, parameter, out errors);
                }
                private bool TryScheduleJob(BackgroundTask job, object? parameter, out ICollection<string> errors) {
                    errors = job.ValidateParameter(parameter).ToArray();
                    if (errors.Any()) return false;

                    var json = parameter == null
                        ? string.Empty
                        : {{UtilityClass.CLASSNAME}}.{{UtilityClass.TO_JSON}}(parameter);

                    var entity = new {{ctx.Config.EntityNamespace}}.{{ENTITY_CLASSNAME}} {
                        {{COL_ID}} = Guid.NewGuid().ToString(),
                        {{COL_NAME}} = job.GetJobName(parameter),
                        {{COL_BATCHTYPE}} = job.BatchTypeId,
                        {{COL_PARAMETERJSON}} = json,
                        {{COL_REQUESTTIME}} = CurrentTime,
                        {{COL_STATE}} = {{ENUM_BGTASKSTATE}}.{{ENUM_BGTASKSTATE_WAITTOSTART}},
                    };
                    DbContext.Add(entity);
                    DbContext.SaveChanges();

                    return true;
                }
                #endregion 非同期処理
                """;
        }

    }
}
