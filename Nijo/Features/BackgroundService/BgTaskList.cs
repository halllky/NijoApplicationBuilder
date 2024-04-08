using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {

        private const string LISTUP = "ls";

        private static string RenderAspControllerListAction(CodeRenderingContext context) {
            var appSrv = new Parts.WebServer.ApplicationService();
            var agg = context.Schema.GetAggregate(GraphNodeId);
            var controller = new Controller(agg.Item);

            return $$"""
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
                """;
        }

        private static SourceFile RenderBgTaskListComponent(CodeRenderingContext context) {
            var agg = context.Schema.GetAggregate(GraphNodeId);
            var controller = new Controller(agg.Item);
            var members = agg.GetMembers().ToArray();
            var columns = DataTableColumn.FromMembers("item", agg, true);

            return new SourceFile {
                FileName = "BackgroundTaskList.tsx",
                RenderContent = context => $$"""
                    import { useEffect, useState } from 'react'
                    import dayjs from 'dayjs'
                    import * as Collection from '../collection'
                    import * as Util from '../util'

                    const VForm = Collection.VerticalForm

                    export const BackgroundTaskList = () => {

                      const { get } = Util.useHttpRequest()
                      const [rows, setRows] = useState<GridRow[]>()

                      useEffect(() => {
                        get(`/{{controller.SubDomain}}/{{LISTUP}}`).then(res => {
                          if (res.ok) setRows(res.data as GridRow[])
                        })
                      }, [])

                      return (
                        <VForm.Container label="バックグラウンドプロセス">
                          <VForm.Item wide>
                            <Collection.DataTable
                              data={rows}
                              columns={COLUMN_DEFS}
                              className="h-64"
                            />
                          </VForm.Item>
                        </VForm.Container>
                      )
                    }

                    type GridRow = {
                    {{members.SelectTextTemplate(m => $$"""
                      {{m.MemberName}}?: {{m.TypeScriptTypename}}
                    """)}}
                    }

                    const COLUMN_DEFS: Collection.ColumnDefEx<Util.TreeNode<GridRow>>[] = [
                    {{WithIndent(columns.SelectTextTemplate(c => c.Render()), "  ")}}
                    ]
                    """,
            };
        }
    }
}
