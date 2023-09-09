using HalApplicationBuilder.CodeRendering.Presentation;
using HalApplicationBuilder.CodeRendering.Searching;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class CreateView : TemplateBase {
        internal CreateView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;

        public override string FileName => "new.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/new";
        internal string Route => $"/{_aggregate.Item.UniqueId}/new";

        protected override string Template() {
            return $$"""
                import { useState, useCallback, useReducer } from 'react';
                import { Link, useNavigate } from 'react-router-dom';
                import { FieldValues, SubmitHandler, useForm, FormProvider } from 'react-hook-form';
                import { BookmarkSquareIcon } from '@heroicons/react/24/outline';
                import * as Components from '../../components';
                import { IconButton, InlineMessageBar, BarMessage } from '../../components';
                import { useHttpRequest } from '../../hooks/useHttpRequest';
                import { useAppContext } from "../../hooks/AppContext"
                import { PageContext, pageContextReducer } from '../../hooks/PageContext'
                import * as AggregateType from '../../{{types.ImportName}}'
                import { {{new FormOfAggregateInstance.Component(_aggregate).ComponentName}} } from './components'

                const defaultValues = AggregateType.{{new types.AggregateInstanceInitializerFunction(_aggregate).FunctionName}}()

                export default function () {

                  const pageContextValue = useReducer(pageContextReducer, { pageIsReadOnly: false })
                  const reactHookFormMethods = useForm({ defaultValues })

                  const navigate = useNavigate()
                  const { post } = useHttpRequest()
                  const [, dispatch] = useAppContext()
                  const [errorMessages, setErrorMessages] = useState<BarMessage[]>([])
                  const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
                    const response = await post<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}>(`{{GetCreateCommandApi()}}`, data)
                    if (response.ok) {
                      dispatch({ type: 'pushMsg', msg: `${response.data.{{AggregateInstanceBase.INSTANCE_NAME}}}を作成しました。` })
                      setErrorMessages([])
                      const encoded = window.encodeURI(response.data.{{AggregateInstanceBase.INSTANCE_KEY}}!)
                      navigate(`{{GetSingleViewUrl()}}/${encoded}`)
                    } else {
                      setErrorMessages([...errorMessages, ...response.errors])
                    }
                  }, [post, navigate, errorMessages, setErrorMessages, dispatch])

                  return (
                    <PageContext.Provider value={pageContextValue}>
                      <FormProvider {...reactHookFormMethods}>
                        <form className="page-content-root" onSubmit={reactHookFormMethods.handleSubmit(onSave)}>
                          <h1 className="flex text-base font-semibold select-none p-1">
                            <Link to="{{GetMultiViewUrl()}}">{{_aggregate.Item.DisplayName}}</Link>&nbsp;新規作成
                          </h1>
                          <div className="flex flex-col space-y-1 p-1 bg-neutral-200">
                            <{{new FormOfAggregateInstance.Component(_aggregate).ComponentName}} />
                          </div>
                          <InlineMessageBar value={errorMessages} onChange={setErrorMessages} />
                          <IconButton fill icon={BookmarkSquareIcon} className="self-start">保存</IconButton>
                        </form>
                      </FormProvider>
                    </PageContext.Provider>
                  )
                }
                """;
        }

        private string GetMultiViewUrl() => new SearchFeature(_aggregate.As<IEFCoreEntity>(), _ctx).ReactPageUrl;
        private string GetSingleViewUrl() => new SingleView(_aggregate, _ctx, asEditView: false).Url;
        private string GetCreateCommandApi() => new Controller(_aggregate.Item, _ctx).CreateCommandApi;
    }
}
