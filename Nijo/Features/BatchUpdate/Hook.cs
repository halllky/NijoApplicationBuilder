using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BatchUpdate {
    partial class BatchUpdateFeature {
        private static SourceFile RenderReactHook(CodeRenderingContext context) {
            var controller = GetBatchUpdateController();
            var availableAggregates = GetAvailableAggregatesOrderByDataFlow(context);

            return new SourceFile {
                FileName = "useBatchUpdate.ts",
                RenderContent = context => $$"""
                    import { useCallback } from 'react'
                    import * as Types from '../autogenerated-types'
                    import { useHttpRequest } from './Http'
                    import { useMsgContext } from './Notification'
                    import { LocalRepositoryState } from './LocalRepository'

                    export type BatchUpdateItem
                    {{availableAggregates.SelectTextTemplate((agg, i) => $$"""
                      {{(i == 0 ? "=" : "|")}} { {{PARAM_DATATYPE}}: '{{GetKey(agg)}}', {{PARAM_ACTION}}: '{{ACTION_ADD}}' | '{{ACTION_MODIFY}}' | '{{ACTION_DELETE}}', {{PARAM_DATA}}: Types.{{new DataClassForSave(agg).TsTypeName}} }
                    """)}}

                    export default () => {
                      const [, dispatchMsg] = useMsgContext()
                      const { post } = useHttpRequest()

                      const scheduleBatchUpdate = useCallback(async ({{PARAM_ITEMS}}: BatchUpdateItem[]) => {
                        const res = await post(`/{{BackgroundService.BgTaskFeature.GetScheduleApiURL(context)}}/{{JOBKEY}}`, { {{PARAM_ITEMS}} })
                        if (!res.ok) {
                          dispatchMsg(msg => msg.error('一括更新に失敗しました。'))
                        }
                      }, [post, dispatchMsg])

                      const batchUpdateImmediately = useCallback(async (Items: BatchUpdateItem[]) => {
                        const res = await post(`/{{controller.SubDomain}}`, { {{PARAM_ITEMS}} })
                        if (!res.ok) {
                          dispatchMsg(msg => msg.error('一括更新に失敗しました。'))
                        }
                      }, [post, dispatchMsg])

                      return { scheduleBatchUpdate, batchUpdateImmediately }
                    }
                    """,
            };
        }
    }
}
