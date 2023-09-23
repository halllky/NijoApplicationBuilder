using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    partial class SingleView : TemplateBase {
        internal enum E_Type {
            Create,
            View,
            Edit,
        }

        internal SingleView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx, E_Type type) {
            _aggregate = aggregate;
            _ctx = ctx;
            _type = type;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        public override string FileName => _type switch {
            E_Type.Create => "new.tsx",
            E_Type.View => "detail.tsx",
            E_Type.Edit => "edit.tsx",
            _ => throw new NotImplementedException(),
        };

        internal string GetUrlStringForReact(IEnumerable<string>? keyVariables = null) {
            return GetUrlStringForReact(_type, keyVariables);
        }
        private string GetUrlStringForReact(E_Type type, IEnumerable<string>? keyVariables = null) {
            switch (type) {
                case E_Type.Create:
                    return $"/{_aggregate.Item.UniqueId}/new";

                case E_Type.View:
                case E_Type.Edit:
                    if (keyVariables == null) throw new ArgumentNullException(nameof(keyVariables));
                    var command = type == E_Type.View ? "detail" : "edit";
                    var encoded = keyVariables.Select(key => $"${{window.encodeURI(`${{{key}}}`)}}");
                    return $"/{_aggregate.Item.UniqueId}/{command}/{encoded.Join("/")}";

                default:
                    throw new NotImplementedException();
            }
        }

        internal string Route => _type switch {
            E_Type.Create => $"/{_aggregate.Item.UniqueId}/new",

            // React Router は全角文字非対応なので key0, key1, ... をURLに使う
            E_Type.View => $"/{_aggregate.Item.UniqueId}/detail/{_aggregate.GetKeys().Select((_, i) => $":key{i}").Join("/")}",
            E_Type.Edit => $"/{_aggregate.Item.UniqueId}/edit/{_aggregate.GetKeys().Select((_, i) => $":key{i}").Join("/")}",

            _ => throw new NotImplementedException(),
        };

        protected override string Template() {
            var controller = new Controller(_aggregate.Item);
            var multiViewUrl = new Searching.SearchFeature(_aggregate.As<IEFCoreEntity>(), _ctx).ReactPageUrl;
            var components = _aggregate
                .EnumerateThisAndDescendants()
                .Select(x => new AggregateComponent(x, _ctx, _type));
            var createEmptyObject = new AggregateInstanceInitializerFunction(_aggregate).FunctionName;

            var find = new FindFeature(_aggregate);

            var keyName = new AggregateKeyName(_aggregate);
            var keysFromUrl = keyName.GetKeys().Select(m => $"urlKey{m.MemberName}").ToArray();

            return $$"""
                import { useState, useCallback, useMemo, useReducer, useRef } from 'react';
                import { Link, useParams, useNavigate } from 'react-router-dom';
                import { FieldValues, SubmitHandler, useForm, FormProvider, useFormContext, useFieldArray } from 'react-hook-form';
                import { BookmarkSquareIcon, PencilIcon, XMarkIcon, PlusIcon } from '@heroicons/react/24/outline';
                import { UUID } from 'uuidjs';
                import * as Components from '../../components';
                import { useAppContext, PageContext, pageContextReducer, usePageContext, visitObject, useHttpRequest } from '../../hooks';
                import * as AggregateType from '../../{{types.ImportName}}'

                export default function () {

                  // コンテキスト等
                  const [, dispatch] = useAppContext()
                  const pageContextValue = useReducer(pageContextReducer, { })
                  const { get, post } = useHttpRequest()
                  const [errorMessages, setErrorMessages] = useState<Components.BarMessage[]>([])

                  // 画面表示時
                {{If(_type == E_Type.Create, () => $$"""
                  const defaultValues = useMemo(() => {
                    return AggregateType.{{createEmptyObject}}()
                  }, [])
                """).Else(() => $$"""
                  const { {{keyName.GetKeys().Select((m, i) => $"key{i}: urlKey{m.MemberName}").Join(", ")}} } = useParams()
                  const [instanceName, setInstanceName] = useState<string | undefined>('')
                  const [fetched, setFetched] = useState(false)
                  const defaultValues = useCallback(async () => {
                {{keysFromUrl.SelectTextTemplate(key => $$"""
                    if ({{key}} == null) return AggregateType.{{createEmptyObject}}()
                """)}}
                    const response = await get({{find.GetUrlStringForReact(keysFromUrl)}})
                    setFetched(true)
                    if (response.ok) {
                      const responseData = response.data as AggregateType.{{_aggregate.Item.TypeScriptTypeName}}
                      setInstanceName({{keyName.GetNames().Select(m => $"String(responseData.{m.MemberName})").Join(" + ")}})

                      visitObject(responseData, obj => {
                        // 新規データのみ主キーを編集可能にするため、読込データと新規データを区別するためのフラグをつける
                        (obj as { {{AggregateDetail.IS_LOADED}}?: boolean }).{{AggregateDetail.IS_LOADED}} = true;
                        // 配列中のオブジェクト識別用
                        (obj as { {{AggregateDetail.OBJECT_ID}}: string }).{{AggregateDetail.OBJECT_ID}} = UUID.generate()
                      })

                      return responseData
                    } else {
                      return AggregateType.{{createEmptyObject}}()
                    }
                  }, [{{keysFromUrl.Join(", ")}}])
                """)}}

                  const reactHookFormMethods = useForm({ defaultValues })

                  // 編集時
                  const formRef = useRef<HTMLFormElement | null>(null)
                  const onKeyDown = useCallback((e: React.KeyboardEvent<HTMLFormElement>) => {
                    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
                      // Ctrl + Enter で送信
                      formRef.current?.dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }))

                    } else if (e.key === 'Enter' && !(e.target as HTMLElement).matches('textarea')) {
                      // フォーム中でEnterキーが押されたときに誤submitされるのを防ぐ。
                      // textareaでpreventDefaultすると改行できなくなるので除外
                      e.preventDefault()

                    }
                  }, [])

                  // 処理確定時
                  const navigate = useNavigate()
                {{If(_type == E_Type.View, () => $$"""
                  const navigateToEditView = useCallback((e: React.MouseEvent) => {
                    navigate(`{{GetUrlStringForReact(E_Type.Edit, keysFromUrl)}}`)
                    e.preventDefault()
                  }, [navigate, {{keysFromUrl.Join(", ")}}])
                """)}}
                {{If(_type == E_Type.Create, () => $$"""
                  const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
                    const response = await post<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}>(`{{controller.CreateCommandApi}}`, data)
                    if (response.ok) {
                      dispatch({ type: 'pushMsg', msg: `${({{keyName.GetNames().Select(m => $"String(response.data.{m.MemberName})").Join(" + ")}})}を作成しました。` })
                      setErrorMessages([])
                      navigate(`{{GetUrlStringForReact(E_Type.View, keyName.GetKeys().Select(m => $"response.data.{m.MemberName}"))}}`)
                    } else {
                      setErrorMessages([...errorMessages, ...response.errors])
                    }
                  }, [post, navigate, errorMessages, setErrorMessages, dispatch])
                """).ElseIf(_type == E_Type.Edit, () => $$"""
                  const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
                    const response = await post<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}>(`{{controller.UpdateCommandApi}}`, data)
                    if (response.ok) {
                      setErrorMessages([])
                      dispatch({ type: 'pushMsg', msg: `${({{keyName.GetNames().Select(m => $"String(response.data.{m.MemberName})").Join(" + ")}})}を更新しました。` })
                      navigate(`{{GetUrlStringForReact(E_Type.View, keysFromUrl)}}`)
                    } else {
                      setErrorMessages([...errorMessages, ...response.errors])
                    }
                  }, [errorMessages, dispatch, post, navigate, {{keysFromUrl.Join(", ")}}])
                """)}}

                {{If(_type == E_Type.View || _type == E_Type.Edit, () => $$"""
                  if (!fetched) return <></>
                """)}}

                  return (
                    <PageContext.Provider value={pageContextValue}>
                      <FormProvider {...reactHookFormMethods}>
                {{If(_type == E_Type.Create || _type == E_Type.Edit, () => $$"""
                        <form className="page-content-root" ref={formRef} onSubmit={reactHookFormMethods.handleSubmit(onSave)} onKeyDown={onKeyDown}>
                """).Else(() => $$"""
                        <form className="page-content-root">
                """)}}
                          <h1 className="flex text-base font-semibold select-none py-1">
                            <Link to="{{multiViewUrl}}">{{_aggregate.Item.DisplayName}}</Link>
                            &nbsp;&#047;&nbsp;
                {{If(_type == E_Type.Create, () => $$"""
                            新規作成
                """).Else(() => $$"""
                            <span className="select-all">{instanceName}</span>
                """)}}
                            <div className="flex-1"></div>
                          </h1>
                          <div className="flex flex-col space-y-1 p-1 bg-neutral-200">
                            {{new AggregateComponent(_aggregate, _ctx, _type).RenderCaller()}}
                          </div>
                          <Components.InlineMessageBar value={errorMessages} onChange={setErrorMessages} />
                {{If(_type == E_Type.Create, () => $$"""
                          <Components.IconButton fill className="self-start" icon={BookmarkSquareIcon}>保存</Components.IconButton>
                """).ElseIf(_type == E_Type.View, () => $$"""
                          <Components.IconButton fill className="self-start" icon={PencilIcon} onClick={navigateToEditView}>編集</Components.IconButton>
                """).ElseIf(_type == E_Type.Edit, () => $$"""
                          <Components.IconButton fill className="self-start" icon={BookmarkSquareIcon}>更新</Components.IconButton>
                """)}}
                        </form>
                      </FormProvider>
                    </PageContext.Provider>
                  )
                }


                {{components.SelectTextTemplate(cmp => cmp.Render())}}
                """;
        }
    }
}
