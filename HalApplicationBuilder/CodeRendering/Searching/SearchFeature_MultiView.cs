using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Searching {
    partial class SearchFeature {
        internal ITemplate CreateReactPage() {
            return new MultiView() {
                Search = this,
            };
        }

        private class MultiView : TemplateBase {

            public override string FileName => REACT_FILENAME;
            public required SearchFeature Search { get; init; }

            protected override string Template() {
                var useQueryKey = $"{Search.PhysicalName}::search";
                var searchApi = $"/{Controller.SUBDOMAIN}/{Search.PhysicalName}/{Controller.SEARCH_ACTION_NAME}";

                var aggregate = Search.DbEntity.Item is Aggregate ? Search.DbEntity.As<Aggregate>() : null;
                var createViewRoute = aggregate == null ? null : new SingleView(aggregate, Search.Context, SingleView.E_Type.Create).Url;
                var singleViewRoute = aggregate == null ? null : new SingleView(aggregate, Search.Context, SingleView.E_Type.View).Url;

                var memberNames = Search.Members.Select(m => m.ConditionPropName);
                var propNameWidth = FormOfAggregateInstance.GetPropNameFlexBasis(memberNames);

                var s = new StringBuilder();
                s.Append($$"""
                    import React, { useState, useCallback } from 'react';
                    import { useCtrlS } from '../../hooks/useCtrlS';
                    import { useAppContext } from '../../hooks/AppContext';
                    import { AgGridReact } from 'ag-grid-react';
                    import { ColDef } from 'ag-grid-community';
                    import { Link, useNavigate } from 'react-router-dom';
                    import { useQuery } from 'react-query';
                    import { FieldValues, SubmitHandler, useForm, FormProvider } from 'react-hook-form';
                    import { BookmarkIcon, ChevronDownIcon, ChevronUpIcon, MagnifyingGlassIcon, PlusIcon } from '@heroicons/react/24/outline';
                    import { IconButton } from '../../components';
                    import { useHttpRequest } from '../../hooks/useHttpRequest';

                    export default function () {

                      const [, dispatch] = useAppContext()
                      useCtrlS(() => {
                        dispatch({ type: 'pushMsg', msg: '保存しました。' })
                      })

                      const { get } = useHttpRequest()
                      const [param, setParam] = useState<FieldValues>({})

                      const reactHookFormMethods = useForm()
                      const register = reactHookFormMethods.register
                      const handleSubmit = reactHookFormMethods.handleSubmit
                      const reset = reactHookFormMethods.reset

                      const onSearch: SubmitHandler<FieldValues> = useCallback(data => {
                        setParam(data)
                      }, [])
                      const onClear = useCallback((e: React.MouseEvent) => {
                        reset()
                        e.preventDefault()
                      }, [reset])
                      const { data, isFetching } = useQuery({
                        queryKey: ['{{useQueryKey}}', JSON.stringify(param)],
                        queryFn: async () => {
                          const response = await get<RowType[]>(`{{searchApi}}`, { param })
                          return response.ok ? response.data : []
                        },
                        onError: error => {
                          dispatch({ type: 'pushMsg', msg: `ERROR!: ${JSON.stringify(error)}` })
                        },
                      })

                      const navigate = useNavigate()
                    {{If(createViewRoute != null, () => $$"""
                      const toCreateView = useCallback(() => {
                        navigate('{{createViewRoute}}')
                      }, [navigate])
                    """)}}

                      const [expanded, setExpanded] = useState(false)

                      if (isFetching) return <></>

                      return (
                        <div className="page-content-root">

                          <div className="flex flex-row justify-start items-center space-x-2">
                            <div className='flex-1 flex flex-row items-center space-x-1 cursor-pointer' onClick={() => setExpanded(!expanded)}>
                              <h1 className="text-base font-semibold select-none py-1">
                                {{Search.DisplayName}}
                              </h1>
                              {expanded
                                ? <ChevronDownIcon className="w-4" />
                                : <ChevronUpIcon className="w-4" />}
                            </div>
                    {{If(createViewRoute != null, () => $$"""
                            <IconButton underline icon={PlusIcon} onClick={toCreateView}>新規作成</IconButton>
                    """)}}
                          </div>

                          <FormProvider {...reactHookFormMethods}>
                            <form className={`${expanded ? '' : 'hidden'} flex flex-col space-y-1 p-1 bg-neutral-200`} onSubmit={handleSubmit(onSearch)}>
                    {{Search.Members.SelectTextTemplate(member => $$"""
                              <div className="flex">
                                <div className="{{propNameWidth}}">
                                  <span className="text-sm select-none opacity-80">
                                    {{member.ConditionPropName}}
                                  </span>
                                </div>
                                <div className="flex-1">
                                  {{TemplateTextHelper.WithIndent(member.Type.RenderUI(new SearchConditionUiForm(member)),"              ")}}
                                </div>
                              </div>
                    """)}}
                              <div className='flex flex-row justify-start space-x-1'>
                                <IconButton fill icon={MagnifyingGlassIcon}>検索</IconButton>
                                <IconButton outline onClick={onClear}>クリア</IconButton>
                                <div className='flex-1'></div>
                                <IconButton underline icon={BookmarkIcon}>この検索条件を保存</IconButton>
                              </div>
                            </form>
                          </FormProvider>

                          <div className="ag-theme-alpine compact flex-1">
                            <AgGridReact
                              rowData={data || []}
                              columnDefs={columnDefs}
                              multiSortKey='ctrl'
                              undoRedoCellEditing
                              undoRedoCellEditingLimit={20}>
                            </AgGridReact>
                          </div>
                        </div>
                      )
                    }

                    type RowType = {
                      {{SEARCHRESULT_INSTANCE_KEY_PROP_NAME}}: string
                    {{Search.Members.SelectTextTemplate(member => $$"""
                      {{member.SearchResultPropName}}?: string | number | boolean
                    """)}}
                    }

                    const columnDefs: ColDef<RowType>[] = [
                    {{If(singleViewRoute != null, () => $$"""
                      {
                        resizable: true,
                        width: 50,
                        cellRenderer: ({ data }: { data: RowType }) => {
                          const encoded = window.encodeURI(data.{{SEARCHRESULT_INSTANCE_KEY_PROP_NAME}})
                          return <Link to={`{{singleViewRoute}}/${encoded}`} className="text-blue-400">詳細</Link>
                        },
                      },
                    """)}}
                    {{Search.Members.SelectTextTemplate(member => $$"""
                      { field: '{{member.SearchResultPropName}}', resizable: true, sortable: true, editable: true },
                    """)}}
                    ]
                    """);

                return s.ToString();
            }

            private class SearchConditionUiForm : IGuiFormRenderer {
                public SearchConditionUiForm(Member member) {
                    _member = member;
                }
                private readonly Member _member;

                private string GetRegisterName(string? inner = null) {
                    var list = _member.CorrespondingDbColumn.Owner
                        .PathFromEntry()
                        .Select(path => path.RelationName)
                        .ToList();
                    list.Add(_member.ConditionPropName);
                    if (!string.IsNullOrEmpty(inner)) list.Add(inner);
                    return list.Join(".");
                }

                /// <summary>
                /// 検索条件: テキストボックス
                /// </summary>
                public string TextBox(bool multiline = false) {
                    if (_member.Type.SearchBehavior == SearchBehavior.Range) {
                        var from = GetRegisterName(Util.FromTo.FROM);
                        var to = GetRegisterName(Util.FromTo.TO);
                        return $$"""
                            <input type="text" className="border w-40" {...register('{{from}}')} />
                            〜
                            <input type="text" className="border w-40" {...register('{{to}}')} />
                            """;

                    } else {
                        var name = GetRegisterName();
                        return $$"""
                            <input type="text" className="border w-80" {...register('{{name}}')} />
                            """;
                    }
                }
                /// <summary>
                /// 検索条件: トグル
                /// </summary>
                public string Toggle() {
                    // TODO: "true only", "false only", "all" の3種類のラジオボタン
                    return string.Empty;
                }
                /// <summary>
                /// 検索条件: 選択肢（コード自動生成時に要素が確定しているもの）
                /// </summary>
                public string Selection(IEnumerable<KeyValuePair<string, string>> options) {
                    return $$"""
                        <select className="border" {...register(`{{GetRegisterName()}}`)}>
                        {{options.SelectTextTemplate(option => $$"""
                          <option value="{{option.Key}}">
                            {{option.Value}}
                          </option>
                        """)}}
                        </select>
                        """;
                }
            }
        }
    }
}
