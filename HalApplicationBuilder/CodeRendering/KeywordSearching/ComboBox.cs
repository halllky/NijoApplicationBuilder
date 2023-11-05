using HalApplicationBuilder.CodeRendering.InstanceHandling;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.KeywordSearching {
    partial class ComboBox {
        internal ComboBox(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ComponentName => $"ComboBox{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        internal string Api => new KeywordSearchingFeature(_aggregate).GetUri();
        internal AggregateKeyName KeyName => new AggregateKeyName(_aggregate);

        internal static string RenderDeclaringFile(IEnumerable<GraphNode<Aggregate>> allAggregates) {
            return $$"""
                import React, { useMemo, useCallback, forwardRef, useImperativeHandle } from "react"
                import { useFormContext } from 'react-hook-form';
                import { useHttpRequest } from "../util"
                import { AsyncComboBox } from "../user-input"
                import * as Types from "../types"

                {{allAggregates.Select(a => new ComboBox(a)).SelectTextTemplate((combo, index) => $$"""
                export const {{combo.ComponentName}} = forwardRef(({ raectHookFormId, readOnly, className, rowIndex }: {
                  // ag-grid CellEditorの場合はrowIndexと組み合わせてこのコンポーネントの中でIDを組み立てる
                  raectHookFormId: string | ((rowIndex: number) => string)
                  readOnly?: boolean
                  className?: string
                  // ag-grid CellEditor用
                  rowIndex?: number
                }, ref) => {

                  const { get } = useHttpRequest()
                  const queryFn = useCallback(async (keyword: string) => {
                    const response = await get<Types.{{combo.KeyName.TypeScriptTypeName}}[]>(`{{combo.Api}}`, { keyword })
                    return response.ok ? response.data : []
                  }, [get])

                  const { watch, setValue } = useFormContext()
                  const rhfId = useMemo(() => {
                    if (typeof raectHookFormId === 'string') {
                      return raectHookFormId
                    } else if (rowIndex !== undefined) {
                      return raectHookFormId(rowIndex)
                    } else {
                      throw Error('IDが関数の場合(グリッドセルの場合)はrowIndex必須')
                    }
                  }, [raectHookFormId, rowIndex])
                  const selectedItem = watch(rhfId)
                  const onSelectedItemChanged = useCallback((item: Types.{{combo.KeyName.TypeScriptTypeName}} | undefined) => {
                    setValue(rhfId, item)
                  }, [setValue, watch])

                  // ag-grid CellEditor用
                  useImperativeHandle(ref, () => ({
                    getValue: () => selectedItem,
                    isCancelBeforeStart: () => false,
                    isCancelAfterEnd: () => false,
                  }))

                  return (
                    <AsyncComboBox
                      value={selectedItem}
                      onChanged={onSelectedItemChanged}
                      keySelector={keySelector{{index}}}
                      textSelector={textSelector{{index}}}
                      queryKey={['combo-{{combo._aggregate.Item.UniqueId}}']}
                      queryFn={queryFn}
                      readOnly={readOnly}
                      className={className}
                    />
                  )
                })

                const keySelector{{index}} = (item: Types.{{combo.KeyName.TypeScriptTypeName}} | null) => {
                  return item
                    ? `{{combo.KeyName.GetKeys().Select(m => "${item." + m.MemberName + "}").Join("::")}}`
                    : ``
                }
                const textSelector{{index}} = (item: Types.{{combo.KeyName.TypeScriptTypeName}} | null) => {
                  return item
                    ? `{{combo.KeyName.GetNames().Select(m => "${item." + m.MemberName + "}").Join("&nbsp;")}}`
                    : ``
                }
                """)}}
                """;
        }

        internal string RenderCaller(string raectHookFormId, params string[] attrs) {
            var attributes = attrs
                .Where(str => !string.IsNullOrWhiteSpace(str))
                .Join(" ");
            return $$"""
                <Input.{{ComponentName}} raectHookFormId={{{raectHookFormId}}} {{attributes}} />
                """;
        }
    }
}
