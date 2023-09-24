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
                import React, { useCallback } from "react"
                import { useFormContext } from 'react-hook-form';
                import { useHttpRequest } from "../hooks/useHttpRequest"
                import { AsyncComboBox } from "../components"
                import { {{keyName.TypeScriptTypeName}} } from "../types"

                export const {{ComponentName}} = ({ raectHookFormId, readOnly, className }: {
                  raectHookFormId: string
                  readOnly?: boolean
                  className?: string
                }) => {

                  const { get } = useHttpRequest()
                  const queryFn = useCallback(async (keyword: string) => {
                    const response = await get<{{keyName.TypeScriptTypeName}}[]>(`{{Api}}`, { keyword })
                    return response.ok ? response.data : []
                  }, [get])

                  const { watch, setValue } = useFormContext()
                  const onSelectedItemChanged = useCallback((item: {{keyName.TypeScriptTypeName}} | null | undefined) => {
                    setValue(raectHookFormId, item)
                  }, [setValue, watch])

                  return (
                    <AsyncComboBox
                      selectedItem={watch(raectHookFormId)}
                      onSelectedItemChanged={onSelectedItemChanged}
                      keySelector={keySelector}
                      textSelector={textSelector}
                      queryKey={queryKey}
                      queryFn={queryFn}
                      readOnly={readOnly}
                      className={className}
                    />
                  )
                }

                const queryKey = ['combo-{{_aggregate.Item.UniqueId}}']
                const keySelector = (item: {{keyName.TypeScriptTypeName}}) => {
                  return `{{keyName.GetKeys().Select(m => "${item." + m.MemberName + "}").Join("::")}}`
                }
                const textSelector = (item: {{keyName.TypeScriptTypeName}}) => {
                  return `{{keyName.GetNames().Select(m => "${item?." + m.MemberName + "}").Join("&nbsp;")}}`
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
