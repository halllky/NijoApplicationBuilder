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
        internal ComboBox(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;

        public override string FileName => $"ComboBox{_aggregate.Item.DisplayName.ToFileNameSafe()}.tsx";

        internal string ComponentName => $"ComboBox{_aggregate.Item.DisplayName.ToCSharpSafe()}";

        internal string UseQueryKey => $"combo-{_aggregate.Item.UniqueId}";
        internal string Api => new KeywordSearchingFeature(_aggregate, _ctx).GetUri();

        protected override string Template() {
            return $$"""
                import React, { forwardRef, ForwardedRef, useState, useCallback } from "react"
                import { useQuery } from "react-query"
                import { useFormContext } from 'react-hook-form';
                import { Combobox } from "@headlessui/react"
                import { ChevronUpDownIcon } from "@heroicons/react/24/outline"
                import { NowLoading } from "./NowLoading"
                import { useAppContext } from "../hooks/AppContext"
                import { useHttpRequest } from "../hooks/useHttpRequest"

                export const {{ComponentName}} = forwardRef(({ raectHookFormId, readOnly }: {
                  raectHookFormId: string
                  readOnly?: boolean
                }, ref: ForwardedRef<HTMLElement>) => {

                  const [keyword, setKeyword] = useState('')
                  const { get } = useHttpRequest()
                  const [, dispatch] = useAppContext()
                  const { data, refetch, isFetching } = useQuery({
                    queryKey: ['{{UseQueryKey}}'],
                    queryFn: async () => {
                      const response = await get<{{AggregateInstanceKeyNamePair.TS_DEF}}[]>(`{{Api}}`, { keyword })
                      return response.ok ? response.data : []
                    },
                    onError: error => {
                      dispatch({ type: 'pushMsg', msg: `ERROR!: ${JSON.stringify(error)}` })
                    },
                  })

                  const [setTimeoutHandle, setSetTimeoutHandle] = useState<NodeJS.Timeout | undefined>(undefined)
                  const onChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
                    setKeyword(e.target.value)
                    if (setTimeoutHandle !== undefined) clearTimeout(setTimeoutHandle)
                    setSetTimeoutHandle(setTimeout(() => {
                      refetch()
                      setSetTimeoutHandle(undefined)
                    }, 300))
                  }, [setKeyword, setTimeoutHandle, setSetTimeoutHandle, refetch])
                  const onBlur = useCallback(() => {
                    if (setTimeoutHandle !== undefined) clearTimeout(setTimeoutHandle)
                    setSetTimeoutHandle(undefined)
                    refetch()
                  }, [setTimeoutHandle, setSetTimeoutHandle, refetch])

                  const { watch, setValue } = useFormContext()
                  const onChangeSelectedValue = useCallback((value?: {{AggregateInstanceKeyNamePair.TS_DEF}}) => {
                    setValue(raectHookFormId, value)
                  }, [setValue, watch])
                  const displayValue = useCallback((item?: {{AggregateInstanceKeyNamePair.TS_DEF}}) => {
                    return item?.name || ''
                  }, [])

                  return (
                    <Combobox ref={ref} value={watch(raectHookFormId) || null} onChange={onChangeSelectedValue} nullable disabled={readOnly}>
                      <div className="relative {{AggregateComponent.INPUT_WIDTH}}">
                        <Combobox.Input displayValue={displayValue} onChange={onChange} onBlur={onBlur} className="w-full" spellCheck="false" autoComplete="off" />
                        {!readOnly &&
                          <Combobox.Button className="absolute inset-y-0 right-0 flex items-center pr-2">
                            <ChevronUpDownIcon className="h-5 w-5 text-gray-400" aria-hidden="true" />
                          </Combobox.Button>}
                        <Combobox.Options className="absolute mt-1 w-full overflow-auto bg-white py-1 shadow-lg focus:outline-none">
                          {(setTimeoutHandle !== undefined || isFetching) &&
                            <NowLoading />}
                          {(setTimeoutHandle === undefined && !isFetching && data?.length === 0) &&
                            <span className="p-1 text-sm select-none opacity-50">データなし</span>}
                          {(setTimeoutHandle === undefined && !isFetching) && data?.map(item => (
                            <Combobox.Option key={item.{{AggregateInstanceKeyNamePair.JSON_KEY}}} value={item}>
                              {({ active }) => (
                                <div className={active ? 'bg-neutral-200' : ''}>
                                  {item.{{AggregateInstanceKeyNamePair.JSON_NAME}}}
                                </div>
                              )}
                            </Combobox.Option>
                          ))}
                        </Combobox.Options>
                      </div>
                    </Combobox>
                  )
                })
                """;
        }

        internal string RenderCaller(string raectHookFormId, bool readOnly) {
            return $$"""
                <Components.{{ComponentName}} raectHookFormId={{{raectHookFormId}}} {{(readOnly ? "readOnly" : "")}} />
                """;
        }
        internal string RenderCaller(string raectHookFormId, string readOnlyCondition) {
            return $$"""
                <Components.{{ComponentName}} raectHookFormId={{{raectHookFormId}}} readOnly={{{readOnlyCondition}}} />
                """;
        }
    }
}
