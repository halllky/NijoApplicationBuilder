using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.Features.InstanceHandling;
using HalApplicationBuilder.Features.Util;
using HalApplicationBuilder.Features.WebClient;
using static HalApplicationBuilder.Features.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.Searching {
    internal class MultiViewField {
        internal required string PhysicalName { get; init; }
        internal required IAggregateMemberType MemberType { get; init; }
        internal required bool VisibleInGui { get; init; }
    }

    internal class MultiView2 {
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
        internal const string SEARCHCONDITION_PAGE_PROP_NAME = "__halapp__Page";

        internal SourceFile RenderMultiView() => new SourceFile {
            FileName = REACT_FILENAME,
            RenderContent = ctx => {
                var useQueryKey = $"{DisplayName.ToCSharpSafe()}::search";
                var searchApi = $"/{Controller.SUBDOMAIN}/{DisplayName.ToCSharpSafe()}/{Controller.SEARCH_ACTION_NAME}";

                var fieldNames = Fields.Where(f => f.VisibleInGui).Select(f => f.PhysicalName);
                var propNameWidth = AggregateComponent.GetPropNameFlexBasis(fieldNames);

                return $$"""
                     import React, { useState, useCallback } from 'react';
                     import { FieldValues, SubmitHandler, useForm, FormProvider } from 'react-hook-form';
                     import { Link, useNavigate } from 'react-router-dom';
                     import { ColDef } from 'ag-grid-community';
                     import { useQuery } from 'react-query';
                     import { BookmarkIcon, MagnifyingGlassIcon, PlusIcon } from '@heroicons/react/24/outline';
                     import { useAppContext } from '../../application';
                     import { IconButton, AgGridWrapper } from '../../user-input';
                     import { useHttpRequest } from '../../util';

                     export default function () {

                       const [{ darkMode }, dispatch] = useAppContext()
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
                               <IconButton underline icon={MagnifyingGlassIcon} onClick={() => setExpanded(!expanded)}>詳細検索</IconButton>
                             </div>
                     {{If(CreateViewUrl != null, () => $$"""
                             <IconButton underline icon={PlusIcon} onClick={toCreateView}>新規作成</IconButton>
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
                                 <IconButton fill icon={MagnifyingGlassIcon}>検索</IconButton>
                                 <IconButton outline onClick={onClear}>クリア</IconButton>
                                 <div className='flex-1'></div>
                                 <IconButton underline icon={BookmarkIcon}>この検索条件を保存</IconButton>
                               </div>
                             </form>
                           </FormProvider>

                           <AgGridWrapper
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

            /// <summary>
            /// 検索条件: テキストボックス
            /// </summary>
            public string TextBox(bool multiline = false) {
                if (_field.MemberType.SearchBehavior == SearchBehavior.Range) {
                    return $$"""
                        <input type="text" className="border w-40" {...register('{{_field.PhysicalName}}.{{Util.FromTo.FROM}}')} />
                        〜
                        <input type="text" className="border w-40" {...register('{{_field.PhysicalName}}.{{Util.FromTo.TO}}')} />
                        """;

                } else {
                    return $$"""
                        <input type="text" className="border w-80" {...register('{{_field.PhysicalName}}')} />
                        """;
                }
            }
            /// <summary>
            /// 検索条件： 数値
            /// </summary>
            public string Number() {
                if (_field.MemberType.SearchBehavior == SearchBehavior.Range) {
                    return $$"""
                        <input type="number" className="border w-40" {...register('{{_field.PhysicalName}}')} />
                        〜
                        <input type="number" className="border w-40" {...register('{{_field.PhysicalName}}')} />
                        """;

                } else {
                    return $$"""
                        <input type="number" className="border w-80" {...register('{{_field.PhysicalName}}')} />
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
                    <select className="border" {...register(`{{_field.PhysicalName}}`)}>
                    {{options.SelectTextTemplate(option => $$"""
                      <option value="{{option.Key}}">
                        {{option.Value}}
                      </option>
                    """)}}
                    </select>
                    """;
            }
            /// <summary>
            /// 検索条件: 隠しフィールド
            /// </summary>
            public string HiddenField() {
                return $$"""
                    <input type="hidden" {...register('{{_field.PhysicalName}}')} />
                    """;
            }
        }

        internal string RenderAspNetController(CodeRenderingContext ctx) {
            var controller = new WebClient.Controller(DisplayName.ToCSharpSafe());

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.AspNetCore.Mvc;
                    using {{ctx.Config.EntityNamespace}};

                    partial class {{controller.ClassName}} {
                        [HttpGet("{{WebClient.Controller.SEARCH_ACTION_NAME}}")]
                        public virtual IActionResult Search([FromQuery] string param) {
                            var json = System.Web.HttpUtility.UrlDecode(param);
                            var condition = string.IsNullOrWhiteSpace(json)
                                ? new {{SearchConditionClassName}}()
                                : {{Utility.CLASSNAME}}.{{Utility.PARSE_JSON}}<{{SearchConditionClassName}}>(json);
                            var searchResult = _applicationService
                                .{{AppSrvMethodName}}(condition)
                                .AsEnumerable();
                            return this.JsonContent(searchResult);
                        }
                    }
                }
                """;
        }

        internal string RenderTypeScriptTypeDef(CodeRenderingContext ctx) {
            return $$"""
                export type {{SearchConditionClassName}} = {
                {{Fields.SelectTextTemplate(field => $$"""
                  {{field.PhysicalName}}?: {{field.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                export type {{SearchResultClassName}} = {
                {{Fields.SelectTextTemplate(field => $$"""
                  {{field.PhysicalName}}?: {{field.MemberType.GetTypeScriptTypeName()}}
                """)}}
                }
                """;
        }

        internal string RenderCSharpTypedef(CodeRenderingContext ctx) {
            return $$"""
                #pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなければなりません

                namespace {{ctx.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;

                    /// <summary>
                    /// {{DisplayName}}の一覧検索処理の検索条件を表すクラスです。
                    /// </summary>
                    public partial class {{SearchConditionClassName}} : {{SEARCHCONDITION_BASE_CLASS_NAME}} {
                {{Fields.SelectTextTemplate(member => If(member.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                        public {{Util.FromTo.CLASSNAME}}<{{member.MemberType.GetCSharpTypeName()}}> {{member.PhysicalName}} { get; set; } = new();
                """).Else(() => $$"""
                        public {{member.MemberType.GetCSharpTypeName()}} {{member.PhysicalName}} { get; set; }
                """))}}
                    }

                    /// <summary>
                    /// {{DisplayName}}の一覧検索処理の検索結果1件を表すクラスです。
                    /// </summary>
                    public partial class {{SearchResultClassName}} {
                {{Fields.SelectTextTemplate(member => $$"""
                        public {{member.MemberType.GetCSharpTypeName()}} {{member.PhysicalName}} { get; set; }
                """)}}
                    }
                }
                """;
        }

        internal static SourceFile RenderCSharpSearchConditionBaseClass() => new SourceFile {
            FileName = "SearchConditionBase.cs",
            RenderContent = ctx => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    public abstract class {{SEARCHCONDITION_BASE_CLASS_NAME}} {
                        public int? {{SEARCHCONDITION_PAGE_PROP_NAME}} { get; set; }
                    }
                }
                """,
        };
    }
}
