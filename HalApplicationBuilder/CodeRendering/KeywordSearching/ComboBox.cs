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
    partial class ComboBox : TemplateBase {
        internal ComboBox(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        public override string FileName => $"ComboBox{_aggregate.Item.DisplayName.ToFileNameSafe()}.tsx";
        internal string ComponentName => $"ComboBox{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        internal string Api => new KeywordSearchingFeature(_aggregate).GetUri();

        protected override string Template() {
            var keyName = new AggregateKeyName(_aggregate);

            return $$"""
                import React, { useMemo, useCallback, forwardRef, useImperativeHandle } from "react"
                import { useFormContext } from 'react-hook-form';
                import { useHttpRequest } from "../hooks/useHttpRequest"
                import { AsyncComboBox } from "../components"
                import { {{keyName.TypeScriptTypeName}} } from "../types"

                export const {{ComponentName}} = forwardRef(({ raectHookFormId, readOnly, className, rowIndex }: {
                  // ag-grid CellEditorの場合はrowIndexと組み合わせてこのコンポーネントの中でIDを組み立てる
                  raectHookFormId: string | ((rowIndex: number) => string)
                  readOnly?: boolean
                  className?: string
                  // ag-grid CellEditor用
                  rowIndex?: number
                }, ref) => {

                  const { get } = useHttpRequest()
                  const queryFn = useCallback(async (keyword: string) => {
                    const response = await get<{{keyName.TypeScriptTypeName}}[]>(`{{Api}}`, { keyword })
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
                  const onSelectedItemChanged = useCallback((item: {{keyName.TypeScriptTypeName}} | null | undefined) => {
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
                      selectedItem={selectedItem}
                      onSelectedItemChanged={onSelectedItemChanged}
                      keySelector={keySelector}
                      textSelector={textSelector}
                      queryKey={queryKey}
                      queryFn={queryFn}
                      readOnly={readOnly}
                      className={className}
                    />
                  )
                })

                const queryKey = ['combo-{{_aggregate.Item.UniqueId}}']
                const keySelector = (item: {{keyName.TypeScriptTypeName}} | null) => {
                  return item
                    ? `{{keyName.GetKeys().Select(m => "${item." + m.MemberName + "}").Join("::")}}`
                    : ``
                }
                const textSelector = (item: {{keyName.TypeScriptTypeName}} | null) => {
                  return item
                    ? `{{keyName.GetNames().Select(m => "${item." + m.MemberName + "}").Join("&nbsp;")}}`
                    : ``
                }
                """;
        }

        internal string RenderCaller(string raectHookFormId, bool readOnly) {
            return $$"""
                <Components.{{ComponentName}} raectHookFormId={{{raectHookFormId}}} {{(readOnly ? "readOnly" : "")}} className="{{AggregateComponent.INPUT_WIDTH}}" />
                """;
        }
        internal string RenderCaller(string raectHookFormId, string readOnlyCondition) {
            return $$"""
                <Components.{{ComponentName}} raectHookFormId={{{raectHookFormId}}} readOnly={{{readOnlyCondition}}} className="{{AggregateComponent.INPUT_WIDTH}}" />
                """;
        }
    }
}
