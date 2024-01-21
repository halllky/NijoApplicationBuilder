using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {

        private string RenderAppSrvMethod(CodeRenderingContext ctx) {

            return $$"""
                public bool TryScheduleJob<TJob>(out ICollection<string> errors) where TJob : BackgroundTask, new() {
                    TrySchedule(null, out errors);
                }
                public bool TryScheduleJob<TJob>(TParameter parameter, out ICollection<string> errors) where TJob : BackgroundTask<TParameter>, new() {
                    TryScheduleJob(parameter, out errors);
                }
                private bool TryScheduleJob<TJob>(object? parameter, out ICollection<string> errors) where TJob : BackgroundTask, new() {
                    var job = new TJob();
                    errors = job.ValidateParameter(parameter).ToArray();
                    if (errors.Any()) return false;

                    var json = parameter == null
                        ? string.Empty
                        : JsonSerializer.Serialize(parameter);

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
                """;
        }

    }
}
