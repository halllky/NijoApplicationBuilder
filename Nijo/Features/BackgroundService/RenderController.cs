using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {
        private SourceFile RenderController() => new SourceFile {
            FileName = "BgTaskController.cs",
            RenderContent = context => {
                var appSrv = new ApplicationService();
                var agg = context.Schema.GetAggregate(GraphNodeId);
                var controller = new Controller(agg.Item);

                return $$"""
                    namespace {{context.Config.RootNamespace}} {
                        using Microsoft.AspNetCore.Mvc;
                        using Microsoft.EntityFrameworkCore;

                        [ApiController]
                        [Route("{{Controller.SUBDOMAIN}}/[controller]")]
                        public partial class {{controller.ClassName}} : ControllerBase {
                            public {{controller.ClassName}}({{appSrv.ClassName}} applicationService) {
                                _applicationService = applicationService;
                            }

                            private readonly {{appSrv.ClassName}} _applicationService;

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

                            [HttpGet("{{LISTUP}}")]
                            public virtual IActionResult Listup(
                                [FromQuery] DateTime? since,
                                [FromQuery] DateTime? until,
                                [FromQuery] int? skip,
                                [FromQuery] int? take) {

                                var query = (IQueryable<{{ENTITY_CLASSNAME}}>)_applicationService.{{appSrv.DbContext}}.{{agg.Item.DbSetName}}.AsNoTracking();

                                // 絞り込み
                                if (since != null) {
                                    var paramSince = since.Value.Date;
                                    query = query.Where(e => e.{{COL_REQUESTTIME}} >= paramSince);
                                }
                                if (until != null) {
                                    var paramUntil = until.Value.Date.AddDays(1);
                                    query = query.Where(e => e.{{COL_REQUESTTIME}} <= paramUntil);
                                }

                                // 順番
                                query = query.OrderByDescending(e => e.{{COL_REQUESTTIME}});

                                // ページング
                                if (skip != null) query = query.Skip(skip.Value);

                                const int DEFAULT_PAGE_SIZE = 20;
                                var pageSize = take ?? DEFAULT_PAGE_SIZE;
                                query = query.Take(pageSize);

                                return this.JsonContent(query.ToArray());
                            }
                        }
                    }
                    """;
            },
        };
    }
}
