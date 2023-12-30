using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Architecture.Utility;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Architecture.WebClient {
    internal class MultiViewField {
        internal required string PhysicalName { get; init; }
        internal required IAggregateMemberType MemberType { get; init; }
        internal required bool VisibleInGui { get; init; }
    }

    internal class MultiView : Infrastructure.IReactPage {

        string Infrastructure.IReactPage.Url => Url;
        string Infrastructure.IReactPage.DirNameInPageDir => DisplayName.ToFileNameSafe();
        string Infrastructure.IReactPage.ComponentPhysicalName => $"{DisplayName.ToCSharpSafe()}MultiView";
        bool Infrastructure.IReactPage.ShowMenu => true;
        string? Infrastructure.IReactPage.LabelInMenu => DisplayName;
        SourceFile Infrastructure.IReactPage.GetSourceFile() => RenderMultiView();


        internal required string DisplayName { get; init; }
        internal required IReadOnlyList<MultiViewField> Fields { get; init; }
        internal required string AppSrvMethodName { get; init; }
        internal required string? CreateViewUrl { get; init; }
        internal required Func<string, string>? SingleViewUrlFunctionBody { get; init; }

        internal const string REACT_FILENAME = "list.tsx";
        internal string Url => $"/{DisplayName.ToHashedString()}";
        internal string SearchConditionClassName => $"{DisplayName.ToCSharpSafe()}SearchCondition";
        internal string SearchResultClassName => $"{DisplayName.ToCSharpSafe()}SearchResult";

        internal const string SEARCHCONDITION_BASE_CLASS_NAME = "SearchConditionBase";
        internal const string SEARCHCONDITION_PAGE_PROP_NAME = "__nijo__Page";

        internal SourceFile RenderMultiView() => new SourceFile {
            FileName = REACT_FILENAME,
            RenderContent = () => {
                var useQueryKey = $"{DisplayName.ToCSharpSafe()}::search";
                var searchApi = $"/{Controller.SUBDOMAIN}/{DisplayName.ToCSharpSafe()}/{Controller.SEARCH_ACTION_NAME}";

                var fieldNames = Fields.Where(f => f.VisibleInGui).Select(f => f.PhysicalName);
                var propNameWidth = AggregateComponent.GetPropNameFlexBasis(fieldNames);

                return $$"""
                    import React, { useState, useCallback } from 'react';
                    import { FieldValues, SubmitHandler, FormProvider } from 'react-hook-form';
                    import { Link, useNavigate } from 'react-router-dom';
                    import { ColDef } from 'ag-grid-community';
                    import { useQuery } from 'react-query';
                    import { BookmarkIcon, MagnifyingGlassIcon, PlusIcon } from '@heroicons/react/24/outline';
                    import { useAppContext } from '../../application';
                    import * as Input from '../../user-input';
                    import { useHttpRequest } from '../../util';
                    import * as AggregateType from '../../types';

                    export default function () {

                      const [{ darkMode }, dispatch] = useAppContext()
                      const { get } = useHttpRequest()
                      const [param, setParam] = useState<FieldValues>({})

                      const reactHookFormMethods = Input.useFormEx<AggregateType.{{SearchConditionClassName}}>({})
                      const { registerEx, handleSubmit, reset } = reactHookFormMethods

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
                    {{If(CreateViewUrl != null, () => $$"""
                      const toCreateView = useCallback(() => {
                        navigate(`{{CreateViewUrl}}`)
                      }, [navigate])
                    """)}}

                      const [expanded, setExpanded] = useState(false)

                      if (isFetching) return <></>

                      return (
                        <div className="page-content-root">

                          <div className="flex flex-row justify-start items-center space-x-2">
                            <div className='flex-1 flex flex-row items-center space-x-1 cursor-pointer'>
                              <h1 className="text-base font-semibold select-none py-1">
                                {{DisplayName}}
                              </h1>
                              <Input.IconButton underline icon={MagnifyingGlassIcon} onClick={() => setExpanded(!expanded)}>詳細検索</Input.IconButton>
                            </div>
                    {{If(CreateViewUrl != null, () => $$"""
                            <Input.IconButton underline icon={PlusIcon} onClick={toCreateView}>新規作成</Input.IconButton>
                    """)}}
                          </div>

                          <FormProvider {...reactHookFormMethods}>
                            <form className={`${expanded ? '' : 'hidden'} flex flex-col space-y-1 p-1 bg-color-ridge`} onSubmit={handleSubmit(onSearch)}>
                    {{Fields.Where(f => f.VisibleInGui).SelectTextTemplate(field => $$"""
                              <div className="flex">
                                <div className="{{propNameWidth}}">
                                  <span className="text-sm select-none opacity-80">
                                    {{field.PhysicalName}}
                                  </span>
                                </div>
                                <div className="flex-1">
                                  {{WithIndent(field.MemberType.RenderUI(new SearchConditionUiForm(field)), "              ")}}
                                </div>
                              </div>
                    """)}}
                              <div className='flex flex-row justify-start space-x-1'>
                                <Input.IconButton fill icon={MagnifyingGlassIcon}>検索</Input.IconButton>
                                <Input.IconButton outline onClick={onClear}>クリア</Input.IconButton>
                                <div className='flex-1'></div>
                                <Input.IconButton underline icon={BookmarkIcon}>この検索条件を保存</Input.IconButton>
                              </div>
                            </form>
                          </FormProvider>

                          <Input.AgGridWrapper
                            rowData={data || []}
                            columnDefs={columnDefs}
                            className="flex-1"
                          />
                        </div>
                      )
                    }

                    type RowType = {
                    {{Fields.SelectTextTemplate(member => $$"""
                      {{member.PhysicalName}}?: string | number | boolean
                    """)}}
                    }

                    const columnDefs: ColDef<RowType>[] = [
                    {{If(SingleViewUrlFunctionBody != null, () => $$"""
                      {
                        resizable: true,
                        width: 50,
                        cellRenderer: ({ data }: { data: RowType }) => {
                          const singleViewUrl = `{{SingleViewUrlFunctionBody!("data")}}`
                          return <Link to={singleViewUrl} className="text-blue-400">詳細</Link>
                        },
                      },
                    """)}}
                    {{Fields.Where(f => f.VisibleInGui).SelectTextTemplate(field => $$"""
                      { field: '{{field.PhysicalName}}', resizable: true, sortable: true },
                    """)}}
                    ]
                    """;
            },
        };
        private class SearchConditionUiForm : IGuiFormRenderer {
            public SearchConditionUiForm(MultiViewField field) {
                _field = field;
            }
            private readonly MultiViewField _field;
            private const string INPUT_WIDTH = AggregateComponent.INPUT_WIDTH;

            public string TextBox(bool multiline = false) {
                if (_field.MemberType.SearchBehavior == SearchBehavior.Range) {
                    return $$"""
                        <Input.Word {...registerEx(`{{_field.PhysicalName}}.{{FromTo.FROM}}`)} className="{{INPUT_WIDTH}}" />
                        〜
                        <Input.Word {...registerEx(`{{_field.PhysicalName}}.{{FromTo.TO}}`)} className="{{INPUT_WIDTH}}" />
                        """;

                } else {
                    return $$"""
                        <Input.Word {...registerEx(`{{_field.PhysicalName}}`)} className="{{INPUT_WIDTH}}" />
                        """;
                }
            }
            public string Number() {
                if (_field.MemberType.SearchBehavior == SearchBehavior.Range) {
                    return $$"""
                        <Input.Num {...registerEx(`{{_field.PhysicalName}}.{{FromTo.FROM}}`)} className="{{INPUT_WIDTH}}" />
                        〜
                        <Input.Num {...registerEx(`{{_field.PhysicalName}}.{{FromTo.TO}}`)} className="{{INPUT_WIDTH}}" />
                        """;

                } else {
                    return $$"""
                        <Input.Num {...registerEx(`{{_field.PhysicalName}}`)} className="{{INPUT_WIDTH}}" />
                        """;
                }
            }
            public string DateTime(IGuiFormRenderer.E_DateType dateType) {
                var componentName = dateType switch {
                    IGuiFormRenderer.E_DateType.Year => "Input.Num",
                    IGuiFormRenderer.E_DateType.YearMonth => "Input.YearMonth",
                    _ => "Input.Date",
                };
                if (_field.MemberType.SearchBehavior == SearchBehavior.Range) {
                    return $$"""
                        <{{componentName}} {...registerEx(`{{_field.PhysicalName}}.{{FromTo.FROM}}`)} className="{{INPUT_WIDTH}}" />
                        〜
                        <{{componentName}} {...registerEx(`{{_field.PhysicalName}}.{{FromTo.TO}}`)} className="{{INPUT_WIDTH}}" />
                        """;

                } else {
                    return $$"""
                        <{{componentName}} {...registerEx(`{{_field.PhysicalName}}`)} className="{{INPUT_WIDTH}}" />
                        """;
                }
            }

            /// <summary>
            /// 検索条件: トグル
            /// </summary>
            public string Toggle() {
                return $$"""
                    <Input.Selection
                      options={['trueのみ', 'falseのみ']}
                      keySelector={item => item}
                      textSelector={item => item}
                    />
                    """;
            }
            /// <summary>
            /// 検索条件: 選択肢（コード自動生成時に要素が確定しているもの）
            /// </summary>
            public string Selection(IEnumerable<KeyValuePair<string, string>> options) {
                return $$"""
                    <Input.SelectionEmitsKey
                      {...registerEx(`{{_field.PhysicalName}}`)}
                      options={[
                    {{options.SelectTextTemplate(option => $$"""
                        '{{option.Value}}' as const,
                    """)}}
                      ]}
                      keySelector={item => item}
                      textSelector={item => item}
                    />
                    """;
            }
            /// <summary>
            /// 検索条件: 隠しフィールド
            /// </summary>
            public string HiddenField() {
                return $$"""
                    <input type="hidden" {...registerEx(`{{_field.PhysicalName}}`)} />
                    """;
            }
        }

        internal string RenderTypeScriptTypeDef(ICodeRenderingContext ctx) {
            return $$"""
                export type {{SearchConditionClassName}} = {
                {{Fields.SelectTextTemplate(field => If(field.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                  {{field.PhysicalName}}?: { {{FromTo.FROM}}?: {{field.MemberType.GetTypeScriptTypeName()}}, {{FromTo.TO}}?: {{field.MemberType.GetTypeScriptTypeName()}} }
                """).Else(() => $$"""
                  {{field.PhysicalName}}?: {{field.MemberType.GetTypeScriptTypeName()}}
                """))}}
                }
                export type {{SearchResultClassName}} = {
                {{Fields.SelectTextTemplate(field => $$"""
                  {{field.PhysicalName}}?: {{field.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                """;
        }

        internal static SourceFile RenderCSharpSearchConditionBaseClass(ICodeRenderingContext ctx) => new SourceFile {
            FileName = "SearchConditionBase.cs",
            RenderContent = () => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    public abstract class {{SEARCHCONDITION_BASE_CLASS_NAME}} {
                        public int? {{SEARCHCONDITION_PAGE_PROP_NAME}} { get; set; }
                    }
                }
                """,
        };
    }
}
