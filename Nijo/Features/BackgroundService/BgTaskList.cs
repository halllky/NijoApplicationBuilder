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

        private static SourceFile RenderBgTaskListComponent(CodeRenderingContext context) {
            var agg = context.Schema.GetAggregate(GraphNodeId);
            var controller = new Controller(agg.Item);
            var members = agg.GetMembers().ToArray();
            var columns = DataTableColumn.FromMembers("GridRow", agg, true);

            return new SourceFile {
                FileName = "BackgroundTaskList.tsx",
                RenderContent = context => $$"""
                    import { useEffect, useState } from 'react'
                    import dayjs from 'dayjs'
                    import * as Layout from '../collection'
                    import * as Util from '../util'

                    const VForm = Layout.VerticalForm

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
                            <Layout.DataTable
                              data={rows}
                              columns={COLUMN_DEFS}
                              className="h-64"
                            />
                          </VForm.Item>
                        </VForm.Container>
                      )
                    }

                    type GridRow = {
                      {{Storing.DataClassForDisplay.OWN_MEMBERS/* この名前のオブジェクトでラッピングする必要は無いが、DataTableColumnの処理がDisplayDataClassの型を前提にしている */}}: {
                    {{members.SelectTextTemplate(m => $$"""
                        {{m.MemberName}}?: {{m.TypeScriptTypename}}
                    """)}}
                      }
                    }

                    const COLUMN_DEFS: Layout.ColumnDefEx<GridRow>[] = [
                    {{WithIndent(columns.SelectTextTemplate(c => c.Render()), "  ")}}
                    ]
                    """,
            };
        }
    }
}
