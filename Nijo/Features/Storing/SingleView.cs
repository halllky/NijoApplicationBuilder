using Nijo.Core;
using Nijo.Features.BatchUpdate;
using Nijo.Parts.Utility;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    public class SingleView : IReactPage {
        public enum E_Type {
            Create,
            View,
            Edit,
        }

        internal SingleView(GraphNode<Aggregate> aggregate, E_Type type) {
            if (!aggregate.IsRoot()) throw new ArgumentException("Descendant aggregate cannot have SingleView.");
            _aggregate = aggregate;
            _type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        private const string URLKEY_TYPE_NEW = "key0"; // Createの場合、ローカルリポジトリのitemKeyをURLで受け取る

        string IReactPage.Url {
            get {
                if (_type == E_Type.Create) {
                    return $"/{_aggregate.Item.UniqueId}/new/:{URLKEY_TYPE_NEW}?";

                } else {
                    // React Router は全角文字非対応なので key0, key1, ... をURLに使う
                    var urlKeys = _aggregate
                        .GetKeys()
                        .OfType<AggregateMember.ValueMember>()
                        .Select((_, i) => $":key{i}");

                    if (_type == E_Type.View) {
                        return $"/{_aggregate.Item.UniqueId}/detail/{urlKeys.Join("/")}";

                    } else if (_type == E_Type.Edit) {
                        return $"/{_aggregate.Item.UniqueId}/edit/{urlKeys.Join("/")}";
                    } else {
                        throw new InvalidOperationException($"SingleViewの種類が不正: {_aggregate.Item}");
                    }
                }
            }
        }
        string IReactPage.DirNameInPageDir => _aggregate.Item.DisplayName.ToFileNameSafe();
        string IReactPage.ComponentPhysicalName => _type switch {
            E_Type.Create => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateView",
            E_Type.View => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}DetailView",
            E_Type.Edit => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}EditView",
            _ => throw new NotImplementedException(),
        };
        bool IReactPage.ShowMenu => false;
        string? IReactPage.LabelInMenu => null;
        SourceFile IReactPage.GetSourceFile() => Render();

        internal string FileName => _type switch {
            E_Type.Create => "new.tsx",
            E_Type.View => "detail.tsx",
            E_Type.Edit => "edit.tsx",
            _ => throw new NotImplementedException(),
        };

        internal const string PAGE_CONTEXT = "SingleViewPageContext";

        internal string GetUrlStringForReact(IEnumerable<string>? keyVariables = null) {
            return GetUrlStringForReact(_type, keyVariables);
        }
        private string GetUrlStringForReact(E_Type type, IEnumerable<string>? keyVariables = null) {
            var command = type switch {
                E_Type.Create => "new",
                E_Type.View => "detail",
                E_Type.Edit => "edit",
                _ => throw new NotImplementedException(),
            };
            var queryParameter = keyVariables?
                .Select(key => $"/${{window.encodeURI(`${{{key}}}`)}}")
                ?? Enumerable.Empty<string>();

            return $"/{_aggregate.Item.UniqueId}/{command}{string.Concat(queryParameter)}";
        }

        internal SourceFile Render() => new SourceFile {
            FileName = FileName,
            RenderContent = context => {
                var controller = new Controller(_aggregate.Item);
                var multiViewUrl = new MultiViewEditable(_aggregate).Url;
                var dataClass = new DisplayDataClass(_aggregate);
                var localRepos = new LocalRepository(_aggregate);

                var keyArray = KeyArray.Create(_aggregate);
                var keys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .ToArray();
                var urlKeysWithMember = keys
                    .ToDictionary(vm => vm.Declared, vm => $"urlKey{vm.MemberName}");

                var names = _aggregate
                    .GetNames()
                    .OfType<AggregateMember.ValueMember>()
                    .Where(vm => vm.DeclaringAggregate == _aggregate)
                    .Select(vm => vm.Declared.GetFullPathAsSingleViewDataClass().Join("?."))
                    .ToArray();

                // -----------------------------------------
                // 集約コンポーネントの宣言
                var rootAggregateList = new List<GraphNode<Aggregate>> { _aggregate };
                rootAggregateList.AddRange(_aggregate
                    .GetReferedEdgesAsSingleKeyRecursively()
                    .Select(edge => edge.Initial));

                var aggregateComponents = new List<AggregateComponent>();
                aggregateComponents.AddRange(rootAggregateList
                    .Select(agg => new AggregateComponent(agg, _type, agg != _aggregate)));
                aggregateComponents.AddRange(rootAggregateList
                    .SelectMany(agg => agg.EnumerateThisAndDescendants())
                    .SelectMany(desc => desc.GetMembers())
                    .OfType<AggregateMember.RelationMember>()
                    // RefやParentを除外する
                    .Where(member => member is AggregateMember.Child
                                  || member is AggregateMember.Children
                                  || member is AggregateMember.VariationItem)
                    .Select(member => new AggregateComponent(member, _type)));

                return $$"""
                    import React, { useState, useEffect, useCallback, useMemo, useReducer, useRef, useId, useContext, createContext } from 'react';
                    import { Link, useParams, useNavigate } from 'react-router-dom';
                    import { SubmitHandler, useForm, FormProvider, useFormContext, useFieldArray } from 'react-hook-form';
                    import { BookmarkSquareIcon, PencilIcon, XMarkIcon, PlusIcon, ArrowUturnLeftIcon } from '@heroicons/react/24/outline';
                    import { UUID } from 'uuidjs';
                    import dayjs from 'dayjs';
                    import * as Input from '../../input';
                    import * as Layout from '../../collection';
                    import * as Util from '../../util';
                    import * as AggregateType from '../../autogenerated-types'

                    const VForm = Layout.VerticalForm

                    export default function () {
                      return (
                        <Util.MsgContextProvider>
                          <Page />
                        </Util.MsgContextProvider>
                      )
                    }

                    const Page = () => {
                    {{If(_type == E_Type.Edit || _type == E_Type.View, () => $$"""
                      const { {{urlKeysWithMember.Select((_, i) => $"key{i}").Join(", ")}} } = useParams()
                      const pkArray: [{{keyArray.Select(k => $"{k.TsType} | undefined").Join(", ")}}] = useMemo(() => {
                    {{urlKeysWithMember.SelectTextTemplate((x, i) => x.Key.Options.MemberType.SearchBehavior == SearchBehavior.Range ? $$"""
                        const numKey{{i}} = Number(key{{i}})
                        const {{x.Value}} = isNaN(numKey{{i}}) ? undefined : numKey{{i}}
                    """ : $$"""
                        const {{x.Value}} = key{{i}}
                    """)}}
                        return [{{urlKeysWithMember.Values.Join(", ")}}]
                      }, [{{urlKeysWithMember.Select((_, i) => $"key{i}").Join(", ")}}])

                      const { load{{(_type == E_Type.View ? "" : ", commit")}} } = Util.{{localRepos.HookName}}(pkArray)

                    """).Else(() => $$"""
                      const { {{URLKEY_TYPE_NEW}}: keyOfNewItem } = useParams()
                      const { load, commit } = Util.{{localRepos.HookName}}(keyOfNewItem as Util.ItemKey | undefined)

                    """)}}
                      const [defaultValues, setDefaultValues] = useState<AggregateType.{{dataClass.TsTypeName}} | undefined>()
                      useEffect(() => {
                        load().then(items => {
                          setDefaultValues(items?.[0])
                        })
                      }, [load])
                    {{If(_type != E_Type.View, () => $$"""

                      const handleCommit: ReturnType<typeof Util.{{localRepos.HookName}}>['commit'] = useCallback(async (...items) => {
                        await commit(...items)
                        const afterCommit = await load()
                        setDefaultValues(afterCommit?.[0])
                      }, [load, commit])
                    """)}}

                      return defaultValues ? (
                        <AfterLoaded
                    {{If(_type == E_Type.Edit || _type == E_Type.View, () => $$"""
                          pkArray={pkArray}
                    """)}}
                          defaultValues={defaultValues}
                    {{If(_type != E_Type.View, () => $$"""
                          commit={handleCommit}
                    """)}}
                        ></AfterLoaded>
                      ) : (
                        <>
                          <Util.InlineMessageList />
                        </>
                      )
                    }

                    const AfterLoaded = ({
                    {{If(_type == E_Type.Edit || _type == E_Type.View, () => $$"""
                      pkArray,
                    """)}}
                      defaultValues,
                    {{If(_type != E_Type.View, () => $$"""
                      commit,
                    """)}}
                    }: {
                    {{If(_type == E_Type.Edit || _type == E_Type.View, () => $$"""
                      pkArray: [{{keyArray.Select(k => $"{k.TsType} | undefined").Join(", ")}}]
                    """)}}
                      defaultValues: AggregateType.{{dataClass.TsTypeName}}
                    {{If(_type != E_Type.View, () => $$"""
                      commit: ReturnType<typeof Util.{{localRepos.HookName}}>['commit']
                    """)}}
                    }) => {

                      const navigate = useNavigate()
                      const reactHookFormMethods = useForm({ defaultValues })
                      const { handleSubmit } = reactHookFormMethods

                    {{If(_type != E_Type.Create, () => $$"""
                      const instanceName = useMemo(() => {
                        return `{{names.Select(n => $"${{defaultValues.{n} ?? ''}}").Join(string.Empty)}}`
                      }, [defaultValues.{{DisplayDataClass.OWN_MEMBERS}}])
                    """)}}

                      const formRef = useRef<HTMLFormElement>(null)
                      const onKeyDown: React.KeyboardEventHandler<HTMLFormElement> = useCallback(e => {
                        if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
                          // Ctrl + Enter で送信
                          formRef.current?.dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }))

                        } else if (e.key === 'Enter' && !(e.target as HTMLElement).matches('textarea')) {
                          // フォーム中でEnterキーが押されたときに誤submitされるのを防ぐ。
                          // textareaでpreventDefaultすると改行できなくなるので除外
                          e.preventDefault()
                        }
                      }, [])

                    {{If(_type == E_Type.View, () => $$"""
                      const navigateToEditView = useCallback((e: React.MouseEvent) => {
                        navigate(`{{GetUrlStringForReact(E_Type.Edit, urlKeysWithMember.Select((_, i) => $"pkArray[{i}]"))}}`)
                        e.preventDefault()
                      }, [navigate, pkArray])

                    """).ElseIf(_type == E_Type.Create && _aggregate.Item.Options.DisableLocalRepository == true, () => $$"""
                      // データ新規作成の直接コミット
                      const [, dispatchToast] = Util.useToastContext()
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        const response = await post<AggregateType.{{dataClass.TsTypeName}}>(`{{controller.CreateCommandApi}}`, data)
                        if (response.ok) {
                          dispatchToast(msg => msg.info(`${({{names.Select(path => $"String(response.data.{path})").Join(" + ")}})}を作成しました。`))
                          navigate(`{{GetUrlStringForReact(E_Type.View, keys.Select(vm => $"response.data.{vm.GetFullPath().Join("?.")}"))}}`)
                        }
                      }, [post, navigate])

                    """).ElseIf(_type == E_Type.Edit && _aggregate.Item.Options.DisableLocalRepository == true, () => $$"""
                      // データ更新の直接コミット
                      const [, dispatchToast] = Util.useToastContext()
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        const response = await post<AggregateType.{{dataClass.TsTypeName}}>(`{{controller.UpdateCommandApi}}`, data)
                        if (response.ok) {
                          dispatchToast(msg => msg.info(`${({{names.Select(path => $"String(response.data.{path})").Join(" + ")}})}を更新しました。`))
                          navigate(`{{GetUrlStringForReact(E_Type.View, urlKeysWithMember.Select((_, i) => $"pkArray[{i}]"))}}`)
                        }
                      }, [post, navigate, pkArray])

                    """).Else(() => $$"""
                      // データの一時保存
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        await commit({ ...data, {{DisplayDataClass.WILL_BE_CHANGED}}: true })
                      }, [commit])

                    """)}}
                      return (
                        <FormProvider {...reactHookFormMethods}>
                    {{If(_type == E_Type.Create || _type == E_Type.Edit, () => $$"""
                          <form className="page-content-root" ref={formRef} onSubmit={handleSubmit(onSave)} onKeyDown={onKeyDown}>
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

                            <Util.InlineMessageList />

                            {{new AggregateComponent(_aggregate, _type, false).RenderCaller()}}

                    {{If(_type != E_Type.View && _aggregate.Item.Options.DisableLocalRepository != true, () => $$"""
                            <Input.IconButton submit fill className="self-start" icon={BookmarkSquareIcon}>一時保存</Input.IconButton>
                    """).ElseIf(_type == E_Type.Create, () => $$"""
                            <Input.IconButton submit fill className="self-start" icon={BookmarkSquareIcon}>保存</Input.IconButton>
                    """).ElseIf(_type == E_Type.View, () => $$"""
                            <Input.IconButton submit fill className="self-start" icon={PencilIcon} onClick={navigateToEditView}>編集</Input.IconButton>
                    """).ElseIf(_type == E_Type.Edit, () => $$"""
                            <Input.IconButton submit fill className="self-start" icon={BookmarkSquareIcon}>更新</Input.IconButton>
                    """)}}
                          </form>
                        </FormProvider>
                      )
                    }

                    {{aggregateComponents.SelectTextTemplate(component => component.RenderDeclaration())}}
                    """;
            },
        };
    }
}
