using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class SingleView : TemplateBase {
        internal SingleView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx, bool asEditView) {
            _ctx = ctx;
            _aggregate = aggregate;
            _asEditView = asEditView;
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly bool _asEditView;

        public override string FileName => _asEditView ? "edit.tsx" : "detail.tsx";
        internal string Url => _asEditView
            ? $"/{_aggregate.Item.UniqueId}/edit"
            : $"/{_aggregate.Item.UniqueId}/detail";
        internal string Route => _asEditView
            ? $"/{_aggregate.Item.UniqueId}/edit/:instanceKey"
            : $"/{_aggregate.Item.UniqueId}/detail/:instanceKey";

        protected override string Template() {
            return $$"""
                import { useState, useCallback, useReducer } from 'react';
                import { useAppContext } from '../../hooks/AppContext';
                import { PageContext, pageContextReducer } from '../../hooks/PageContext'
                import { Link, useParams, useNavigate } from 'react-router-dom';
                import { FieldValues, SubmitHandler, useForm, FormProvider } from 'react-hook-form';
                import { BookmarkSquareIcon, PencilIcon } from '@heroicons/react/24/outline';
                import * as Components from '../../components';
                import { IconButton, InlineMessageBar, BarMessage } from '../../components';
                import { useHttpRequest } from '../../hooks/useHttpRequest';
                import * as AggregateType from '../../{{types.ImportName}}'
                import { {{new FormOfAggregateInstance.Component(_aggregate).ComponentName}} } from './components'

                export default function () {

                  const { instanceKey } = useParams()
                  const [, dispatch] = useAppContext()

                  const navigate = useNavigate()
                {{If(!_asEditView, () => $$"""
                  const navigateToEditView = useCallback((e: React.MouseEvent) => {
                    navigate(`{{GetEditViewUrl()}}/${instanceKey}`)
                    e.preventDefault()
                  }, [navigate, instanceKey])
                """)}}

                  const { get, post } = useHttpRequest()
                  const [instanceName, setInstanceName] = useState<string | undefined>('')
                  const [fetched, setFetched] = useState(false)
                  const defaultValues = useCallback(async () => {
                    if (!instanceKey) return AggregateType.{{new types.AggregateInstanceInitializerFunction(_aggregate).FunctionName}}()
                    const encoded = window.encodeURI(instanceKey)
                    const response = await get(`{{GetFindCommandApi()}}/${encoded}`)
                    setFetched(true)
                    if (response.ok) {
                      const responseData = response.data as AggregateType.{{_aggregate.Item.TypeScriptTypeName}}
                      setInstanceName(responseData.{{AggregateInstanceBase.INSTANCE_NAME}})
                      return responseData
                    } else {
                      return AggregateType.{{new types.AggregateInstanceInitializerFunction(_aggregate).FunctionName}}()
                    }
                  }, [instanceKey])

                  const reactHookFormMethods = useForm({ defaultValues })

                  const [errorMessages, setErrorMessages] = useState<BarMessage[]>([])
                  const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
                    const response = await post<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}>(`{{GetUpdateCommandApi()}}`, data)
                    if (response.ok) {
                      setErrorMessages([])
                      dispatch({ type: 'pushMsg', msg: `${response.data.{{AggregateInstanceBase.INSTANCE_NAME}}}を更新しました。` })
                      navigate(`{{GetReadonlySingleViewUrl()}}/${instanceKey}`)
                    } else {
                      setErrorMessages([...errorMessages, ...response.errors])
                    }
                  }, [errorMessages, dispatch, post, navigate, instanceKey])

                  const pageContextValue = useReducer(pageContextReducer, { pageIsReadOnly: {{(_asEditView ? "false" : "true")}} })

                  if (!fetched) return <></>

                  return (
                    <PageContext.Provider value={pageContextValue}>
                      <FormProvider {...reactHookFormMethods}>
                        <form className="page-content-root" onSubmit={reactHookFormMethods.handleSubmit(onSave)}>
                          <h1 className="flex text-base font-semibold select-none p-1">
                            <Link to="{{GetMultiViewUrl()}}">{{_aggregate.Item.DisplayName}}</Link>
                            &nbsp;&#047;&nbsp;
                            <span className="select-all">{instanceName}</span>
                            <div className="flex-1"></div>
                          </h1>
                          <div className="flex flex-col space-y-1 p-1 bg-neutral-200">
                            <{{new FormOfAggregateInstance.Component(_aggregate).ComponentName}} />
                          </div>
                          <InlineMessageBar value={errorMessages} onChange={setErrorMessages} />
                {{If(_asEditView, () => $$"""
                          <IconButton fill className="self-start" icon={BookmarkSquareIcon}>更新</IconButton>
                """).Else(() => $$"""
                          <IconButton fill className="self-start" icon={PencilIcon} onClick={navigateToEditView}>編集</IconButton>
                """)}}
                        </form>
                      </FormProvider>
                    </PageContext.Provider>
                  )
                }
                """;
        }

        private string GetMultiViewUrl() => new Searching.SearchFeature(_aggregate.As<IEFCoreEntity>(), _ctx).ReactPageUrl;
        private string GetEditViewUrl() => new SingleView(_aggregate, _ctx, asEditView: true).Url;
        private string GetReadonlySingleViewUrl() => new SingleView(_aggregate, _ctx, asEditView: false).Url;
        private string GetFindCommandApi() => new Controller(_aggregate.Item, _ctx).FindCommandApi;
        private string GetUpdateCommandApi() => new Controller(_aggregate.Item, _ctx).UpdateCommandApi;
    }
}
