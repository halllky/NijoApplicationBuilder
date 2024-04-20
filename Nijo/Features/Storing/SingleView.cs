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

        private const string KEY0 = "key0"; // Createの場合、ローカルリポジトリのitemKeyをURLで受け取る

        string IReactPage.Url {
            get {
                if (_type == E_Type.Create) {
                    return $"/{_aggregate.Item.UniqueId}/new/:{KEY0}?";

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
                var createEmptyObject = new TSInitializerFunction(_aggregate).FunctionName;
                var findMany = new FindManyFeature(_aggregate);
                var dataClass = new DisplayDataClass(_aggregate);
                var localRepos = new LocalRepository(_aggregate);

                var keyArray = KeyArray.Create(_aggregate);
                var keys = _aggregate
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .ToArray();
                var urlKeys = _type == E_Type.Create
                    ? new[] { KEY0 }
                    : keys.Select(m => $"urlKey{m.MemberName}").ToArray();
                var urlKeysWithMember = keys
                    .ToDictionary(vm => vm.Declared, vm => $"urlKey{vm.MemberName}");

                var refRepositories = dataClass
                    .GetRefFromPropsRecursively()
                    .DistinctBy(p => p.Item1.MainAggregate)
                    .Select(p => new {
                        Repos = new LocalRepository(p.Item1.MainAggregate),
                        FindMany = new FindManyFeature(p.Item1.MainAggregate),
                        Aggregate = p.Item1.MainAggregate,
                        DataClassProp = p,
                        RootAggregateMembersForLoad = p.Item1.MainAggregate
                            .AsEntry()
                            .GetKeys()
                            .OfType<AggregateMember.ValueMember>(),
                    })
                    .ToArray();

                var names = _aggregate
                    .GetNames()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(vm => vm.Declared.GetFullPath().Join("?."))
                    .ToArray();

                var maxIndent = _aggregate
                    .EnumerateDescendants()
                    .Select(a => a.EnumerateAncestors().Count())
                    .DefaultIfEmpty()
                    .Max();

                // -----------------------------------------
                // 左列の横幅の計算
                const decimal INDENT_WIDTH = 1.5m;
                var headersWidthRem = _aggregate
                    .EnumerateThisAndDescendants()
                    .SelectMany(
                        a => new TransactionScopeDataClass(a)
                            .GetOwnMembers()
                            .Where(m => {
                                // 同じ行に値を表示せず、名前が長くても行の横幅いっぱい占有できるため、除外
                                if (m is AggregateMember.Child) return false;
                                if (m is AggregateMember.Children) return false;
                                if (m is AggregateMember.Variation) return false;

                                // 画面上にメンバー名が表示されないため除外
                                if (m is AggregateMember.VariationItem) return false;
                                if (m is AggregateMember.ValueMember vm && vm.Options.InvisibleInGui) return false;

                                return true;
                            }),
                        (a, m) => new {
                            m.MemberName,
                            IndentWidth = a.EnumerateAncestors().Count() * INDENT_WIDTH, // インデント1個の幅をだいたい1.5remとして計算
                            NameWidthRem = m.MemberName.CalculateCharacterWidth() / 2 * 1.2m, // tailwindの1.2remがだいたい全角文字1文字分
                        });
                // インデント込みで最も横幅が長いメンバーの横幅を計算
                var longestHeaderWidthRem = headersWidthRem
                    .Select(x => Math.Ceiling((x.IndentWidth + x.NameWidthRem) * 10m) / 10m)
                    .DefaultIfEmpty()
                    .Max();
                // - longestHeaderWidthRemにはインデントの横幅も含まれているのでインデントの横幅を引く
                // - ヘッダ列の横幅にちょっと余裕をもたせるために+8
                var indentWidth = maxIndent * INDENT_WIDTH;
                var headerWidth = Math.Max(indentWidth, longestHeaderWidthRem - indentWidth) + 8m;

                // -----------------------------------------
                // 集約コンポーネントの宣言
                var rootAggregateList = new List<GraphNode<Aggregate>> { _aggregate };
                rootAggregateList.AddRange(_aggregate
                    .GetReferedEdgesAsSingleKeyRecursively()
                    .Select(edge => edge.Initial));

                var aggregateComponents = new List<AggregateComponent>();
                aggregateComponents.AddRange(rootAggregateList
                    .Select(agg => new AggregateComponent(agg, _type)));
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
                    import React, { useState, useEffect, useCallback, useMemo, useReducer, useRef, useId } from 'react';
                    import { Link, useParams, useNavigate } from 'react-router-dom';
                    import { SubmitHandler, useForm, FormProvider, useFormContext, useFieldArray } from 'react-hook-form';
                    import { BookmarkSquareIcon, PencilIcon, XMarkIcon, PlusIcon } from '@heroicons/react/24/outline';
                    import { UUID } from 'uuidjs';
                    import dayjs from 'dayjs';
                    import * as Input from '../../input';
                    import * as Layout from '../../collection';
                    import * as Util from '../../util';
                    import * as AggregateType from '../../autogenerated-types'

                    const VForm = Layout.VerticalForm

                    export default function () {
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

                      // {{_aggregate.Item.DisplayName}}データの読み込み
                      const {{_aggregate.Item.ClassName}}Repository = Util.{{localRepos.HookName}}(pkArray)
                      const {{_aggregate.Item.ClassName}}IsLoaded = {{_aggregate.Item.ClassName}}Repository.ready
                      const items{{_aggregate.Item.ClassName}} = {{_aggregate.Item.ClassName}}Repository.items

                      // {{_aggregate.Item.DisplayName}}を参照するデータの読み込み
                    {{refRepositories.SelectTextTemplate(x => x.DataClassProp.IsArray ? $$"""
                      const {{x.Aggregate.Item.ClassName}}filter: { filter: AggregateType.{{x.FindMany.TypeScriptConditionClass}} } = useMemo(() => {
                        const filter = AggregateType.{{x.FindMany.TypeScriptConditionInitializerFn}}()
                    {{x.RootAggregateMembersForLoad.Where(vm => urlKeysWithMember.ContainsKey(vm.Declared)).SelectTextTemplate((kv, i) => $$"""
                    {{If(kv.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                        if (filter.{{kv.Declared.GetFullPath().Join("?.")}} !== undefined)
                          filter.{{kv.Declared.GetFullPath().Join(".")}}.{{FromTo.FROM}} = filter.{{kv.Declared.GetFullPath().Join(".")}}.{{FromTo.TO}} = pkArray[{{i}}]
                    """).Else(() => $$"""
                        if (filter.{{kv.Declared.GetFullPath().SkipLast(1).Join("?.")}} !== undefined)
                          filter.{{kv.Declared.GetFullPath().Join(".")}} = pkArray[{{i}}]
                    """)}}
                    """)}}
                        return { filter }
                      }, [pkArray])
                      const {{x.Aggregate.Item.ClassName}}Repository = Util.{{x.Repos.HookName}}({{x.Aggregate.Item.ClassName}}filter)
                      const {{x.Aggregate.Item.ClassName}}IsLoaded = {{x.Aggregate.Item.ClassName}}Repository.ready
                      const items{{x.Aggregate.Item.ClassName}} = {{x.Aggregate.Item.ClassName}}Repository.items

                    """ : $$"""
                      const {{x.Aggregate.Item.ClassName}}Repository = Util.{{x.Repos.HookName}}(pkArray)
                      const {{x.Aggregate.Item.ClassName}}IsLoaded = {{x.Aggregate.Item.ClassName}}Repository.ready
                      const items{{x.Aggregate.Item.ClassName}} = {{x.Aggregate.Item.ClassName}}Repository.items

                    """)}}
                      const allDataLoaded = {{_aggregate.Item.ClassName}}IsLoaded{{string.Concat(refRepositories.Select(x => $" && {x.Aggregate.Item.ClassName}IsLoaded"))}}

                      // 読み込んだデータを画面に表示する形に変換する
                      const defaultValues: AggregateType.{{dataClass.TsTypeName}} | undefined = useMemo(() => {
                        if (allDataLoaded && items{{_aggregate.Item.ClassName}}.length > 0) {
                          return AggregateType.{{dataClass.ConvertFnNameToDisplayDataType}}(items{{_aggregate.Item.ClassName}}[0]{{refRepositories.Select(x => $", items{x.Aggregate.Item.ClassName}").Join("")}})
                        } else {
                          return undefined
                        }
                      }, [allDataLoaded, items{{_aggregate.Item.ClassName}}{{refRepositories.Select(x => $", items{x.Aggregate.Item.ClassName}").Join("")}}])

                      const localReposItemKey = useMemo(() => {
                        if (!{{_aggregate.Item.ClassName}}IsLoaded) return undefined
                        const item = items{{_aggregate.Item.ClassName}}[0]?.item
                        if (!item) return undefined
                        return JSON.stringify([{{keys.Select(k => $"item.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]) as Util.ItemKey
                      }, [{{_aggregate.Item.ClassName}}IsLoaded, items{{_aggregate.Item.ClassName}}])

                      return defaultValues ? (
                        <AfterLoaded
                          pkArray={pkArray}
                          defaultValues={defaultValues}
                          localReposItemKey={localReposItemKey}
                          {{_aggregate.Item.ClassName}}RepositoryModifier={{{_aggregate.Item.ClassName}}Repository}
                    {{refRepositories.SelectTextTemplate(x => $$"""
                          {{x.Aggregate.Item.ClassName}}RepositoryModifier={{{x.Aggregate.Item.ClassName}}Repository}
                    """)}}
                        ></AfterLoaded>
                      ) : (
                        <></>
                      )
                    }

                    const AfterLoaded = ({
                      pkArray,
                      localReposItemKey,
                      defaultValues,
                      {{_aggregate.Item.ClassName}}RepositoryModifier,
                    {{refRepositories.SelectTextTemplate(x => $$"""
                      {{x.Aggregate.Item.ClassName}}RepositoryModifier,
                    """)}}
                    }: {
                      pkArray: [{{keyArray.Select(k => $"{k.TsType} | undefined").Join(", ")}}]
                      localReposItemKey: Util.ItemKey | undefined
                      defaultValues: AggregateType.{{dataClass.TsTypeName}}
                      {{_aggregate.Item.ClassName}}RepositoryModifier: ReturnType<typeof Util.{{localRepos.HookName}}>
                    {{refRepositories.SelectTextTemplate(x => $$"""
                      {{x.Aggregate.Item.ClassName}}RepositoryModifier: ReturnType<typeof Util.{{x.Repos.HookName}}>
                    """)}}
                    }) => {
                      const {
                        add: addTo{{_aggregate.Item.ClassName}}Repository,
                        update: update{{_aggregate.Item.ClassName}}RepositoryItem,
                        remove: remove{{_aggregate.Item.ClassName}}RepositoryItem,
                      } = {{_aggregate.Item.ClassName}}RepositoryModifier
                    {{refRepositories.SelectTextTemplate(x => $$"""
                      const {
                        add: addTo{{x.Aggregate.Item.ClassName}}Repository,
                        update: update{{x.Aggregate.Item.ClassName}}RepositoryItem,
                        remove: remove{{x.Aggregate.Item.ClassName}}RepositoryItem,
                      } = {{x.Aggregate.Item.ClassName}}RepositoryModifier
                    """)}}

                      const navigate = useNavigate()
                      const reactHookFormMethods = useForm({ defaultValues })
                      const { handleSubmit } = reactHookFormMethods

                    {{If(_type != E_Type.Create, () => $$"""
                      const instanceName = useMemo(() => {
                        return `{{names.Select(n => $"${{defaultValues.{DisplayDataClass.OWN_MEMBERS}.{n}}}").Join(string.Empty)}}`
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
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        const response = await post<AggregateType.{{dataClass.TsTypeName}}>(`{{controller.CreateCommandApi}}`, data)
                        if (response.ok) {
                          dispatchMsg(msg => msg.info(`${({{names.Select(path => $"String(response.data.{path})").Join(" + ")}})}を作成しました。`))
                          navigate(`{{GetUrlStringForReact(E_Type.View, keys.Select(m => TransactionScopeDataClass.GetPathOf("response.data", _aggregate, m).Join("?.")))}}`)
                        }
                      }, [post, navigate])

                    """).ElseIf(_type == E_Type.Edit && _aggregate.Item.Options.DisableLocalRepository == true, () => $$"""
                      // データ更新の直接コミット
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        const response = await post<AggregateType.{{dataClass.TsTypeName}}>(`{{controller.UpdateCommandApi}}`, data)
                        if (response.ok) {
                          dispatchMsg(msg => msg.info(`${({{names.Select(path => $"String(response.data.{path})").Join(" + ")}})}を更新しました。`))
                          navigate(`{{GetUrlStringForReact(E_Type.View, urlKeysWithMember.Select((_, i) => $"pkArray[{i}]"))}}`)
                        }
                      }, [post, navigate, pkArray])

                    """).ElseIf(_type == E_Type.Create, () => $$"""
                      // 新規作成データの一時保存
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        if (localReposItemKey === undefined) {
                          const [
                            item{{_aggregate.Item.ClassName}}{{dataClass.GetRefFromPropsRecursively().Select((x, i) => $", item{i}_{x.Item1.MainAggregate.Item.ClassName}").Join("")}}
                          ] = AggregateType.{{dataClass.ConvertFnNameToLocalRepositoryType}}(data)

                          const { itemKey: mainObjectItemKey } = await addTo{{_aggregate.Item.ClassName}}Repository(item{{_aggregate.Item.ClassName}}.item)
                    {{dataClass.GetRefFromPropsRecursively().SelectTextTemplate((x, i) => x.IsArray ? $$"""
                          for (let { itemKey, item } of item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) {
                            await update{{x.Item1.MainAggregate.Item.ClassName}}RepositoryItem(itemKey, item)
                          }
                    """ : $$"""
                          if (item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) {
                            await update{{x.Item1.MainAggregate.Item.ClassName}}RepositoryItem(item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}.itemKey, item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}.item)
                          }
                    """)}}

                          navigate(`{{GetUrlStringForReact(E_Type.Create, new[] { "mainObjectItemKey" })}}`)
                        } else {
                          const [
                            item{{_aggregate.Item.ClassName}}{{dataClass.GetRefFromPropsRecursively().Select((x, i) => $", item{i}_{x.Item1.MainAggregate.Item.ClassName}").Join("")}}
                          ] = AggregateType.{{dataClass.ConvertFnNameToLocalRepositoryType}}(data)

                          await update{{_aggregate.Item.ClassName}}RepositoryItem(localReposItemKey, item{{_aggregate.Item.ClassName}}.item)

                    {{dataClass.GetRefFromPropsRecursively().SelectTextTemplate((x, i) => x.IsArray ? $$"""
                          for (let { itemKey, item } of item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) {
                            await update{{x.Item1.MainAggregate.Item.ClassName}}RepositoryItem(itemKey, item)
                          }
                    """ : $$"""
                          if (item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) {
                            await update{{x.Item1.MainAggregate.Item.ClassName}}RepositoryItem(item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}.itemKey, item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}.item)
                          }
                    """)}}
                        }
                      }, [localReposItemKey, addTo{{_aggregate.Item.ClassName}}Repository, update{{_aggregate.Item.ClassName}}RepositoryItem, navigate])

                    """).ElseIf(_type == E_Type.Edit, () => $$"""
                      // 更新データの一時保存
                      const onSave: SubmitHandler<AggregateType.{{dataClass.TsTypeName}}> = useCallback(async data => {
                        const [
                          item{{_aggregate.Item.ClassName}}{{dataClass.GetRefFromPropsRecursively().Select((x, i) => $", item{i}_{x.Item1.MainAggregate.Item.ClassName}").Join("")}}
                        ] = AggregateType.{{dataClass.ConvertFnNameToLocalRepositoryType}}(data)
                    
                        await update{{_aggregate.Item.ClassName}}RepositoryItem(item{{_aggregate.Item.ClassName}}.itemKey, item{{_aggregate.Item.ClassName}}.item)

                    {{dataClass.GetRefFromPropsRecursively().SelectTextTemplate((x, i) => x.IsArray ? $$"""
                        for (let { itemKey, item } of item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) {
                          await update{{x.Item1.MainAggregate.Item.ClassName}}RepositoryItem(itemKey, item)
                        }
                    """ : $$"""
                        if (item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) {
                          await update{{x.Item1.MainAggregate.Item.ClassName}}RepositoryItem(item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}.itemKey, item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}.item)
                        }
                    """)}}
                      }, [update{{_aggregate.Item.ClassName}}RepositoryItem{{refRepositories.Select(x => $", update{x.Aggregate.Item.ClassName}RepositoryItem").Join("")}}])

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

                            <VForm.Container leftColumnMinWidth="{{headerWidth}}rem">
                              {{new AggregateComponent(_aggregate, _type).RenderCaller()}}
                            </VForm.Container>

                    {{If(_type != E_Type.View && _aggregate.Item.Options.DisableLocalRepository != true, () => $$"""
                            <Input.IconButton fill className="self-start" icon={BookmarkSquareIcon}>一時保存</Input.IconButton>
                    """).ElseIf(_type == E_Type.Create, () => $$"""
                            <Input.IconButton fill className="self-start" icon={BookmarkSquareIcon}>保存</Input.IconButton>
                    """).ElseIf(_type == E_Type.View, () => $$"""
                            <Input.IconButton fill className="self-start" icon={PencilIcon} onClick={navigateToEditView}>編集</Input.IconButton>
                    """).ElseIf(_type == E_Type.Edit, () => $$"""
                            <Input.IconButton fill className="self-start" icon={BookmarkSquareIcon}>更新</Input.IconButton>
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
