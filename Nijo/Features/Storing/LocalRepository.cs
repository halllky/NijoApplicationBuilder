using Nijo.Core;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    internal class LocalRepository {

        internal LocalRepository(GraphNode<Aggregate> aggregate) {
            Aggregate = aggregate;
        }
        internal GraphNode<Aggregate> Aggregate { get; }

        /// <summary>
        /// ローカルリポジトリ内にあるデータそれぞれに割り当てられる、そのデータの種類が何かを識別する文字列
        /// </summary>
        internal string DataTypeKey => Aggregate.Item.ClassName;
        /// <summary>
        /// 永続化層の抽象のフック。外部とのインターフェースの型は <see cref="DisplayDataClass"/>
        /// </summary>
        internal string HookName => $"use{Aggregate.Item.ClassName}Repository";

        /// <summary>
        /// ローカルリポジトリ内のデータとDB上のデータの両方を参照し
        /// ローカルリポジトリにデータがあればそちらを優先して返すフック
        /// </summary>
        internal static SourceFile RenderUseAggregateLocalRepository() {
            return new SourceFile {
                FileName = "LocalRepository.Wrappers.ts",
                RenderContent = ctx => {
                    var aggregates = ctx.Schema
                        .RootAggregatesOrderByDataFlow()
                        .Where(agg => agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key
                                   || agg.Item.Options.Handler == NijoCodeGenerator.Models.ReadModel.Key);

                    var localReposWrapperHooks = aggregates.SelectTextTemplate(agg => {
                        var localRepositosy = new LocalRepository(agg);
                        var displayData = new DisplayDataClass(agg);
                        var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                        // TODO 参照先の名前の表示処理をちゃんとする
                        var names = agg.GetNames().OfType<AggregateMember.ValueMember>().Where(x => x.DeclaringAggregate == agg);
                        var keyArray = KeyArray.Create(agg);
                        var find = new FindFeature(agg);
                        var findMany = new FindManyFeature(agg);

                        var commitable = agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key;

                        var refRepositories = displayData
                            .GetRefFromPropsRecursively()
                            .DistinctBy(x => x.Item1.MainAggregate)
                            .Select(p => new {
                                Repos = new LocalRepository(p.Item1.MainAggregate),
                                FindMany = new FindManyFeature(p.Item1.MainAggregate),
                                Aggregate = p.Item1.MainAggregate,
                                DataClassProp = p,

                                // この画面のメイン集約を参照する関連集約をまとめて読み込むため、
                                // SingleViewのURLのキーで関連集約のAPIへの検索をかけたい。
                                // そのために当該検索条件のうち関連集約の検索に関係するメンバーの一覧
                                RootAggregateMembersForSingleViewLoading = p.Item1.MainAggregate
                                    .GetEntryReversing()
                                    .As<Aggregate>()
                                    .GetMembers()
                                    .OfType<AggregateMember.ValueMember>()
                                    .Where(vm => keyArray.Any(k => k.Member.Declared == vm.Declared)),

                                // この画面のメイン集約を参照する関連集約をまとめて読み込むため、
                                // MultiViewの画面上部の検索条件の値で関連集約のAPIへの検索をかけたい。
                                // そのために当該検索条件のうち関連集約の検索に関係するメンバーの一覧
                                RootAggregateMembersForLoad = p.Item1.MainAggregate
                                    .GetEntryReversing()
                                    .As<Aggregate>()
                                    .GetMembers()
                                    .OfType<AggregateMember.ValueMember>()
                                    // TODO: 検索条件クラスではVariationはbool型で生成されるが
                                    // FindManyFeatureでそれも考慮してメンバーを列挙してくれるメソッドがないので
                                    // 暫定的に除外する（修正後は 011_ダブル.xml で確認可能）
                                    .Where(vm => vm is not AggregateMember.Variation),
                            })
                            .ToArray();

                        // メイン集約を参照する関連集約をまとめて読み込むため、検索条件の値で関連集約のAPIへの検索をかけたい。
                        // そのために検索条件の各項目が関連集約のどの項目と対応するかを調べて返すための関数
                        AggregateMember.ValueMember FindRootAggregateSearchConditionMember(AggregateMember.ValueMember refSearchConditionMember) {
                            var refPath = refSearchConditionMember.DeclaringAggregate.PathFromEntry();
                            var matched = findMany
                                .EnumerateSearchConditionMembers()
                                .Where(kv2 => kv2.Declared == refSearchConditionMember.Declared
                                            // ある集約から別の集約へ複数経路の参照がある場合は対応するメンバーが複数とれてしまうのでパスの後方一致でも絞り込む
                                            && refPath.EndsWith(kv2.Owner.PathFromEntry()))
                                .ToArray();
                            return matched.Single();
                        }

                        return $$"""
                            /** {{agg.Item.DisplayName}}データの読み込みと保存を行います。 */
                            export const {{localRepositosy.HookName}} = (editRange?
                              // データ新規作成の場合
                              : ItemKey
                              // 複数件編集の場合
                              | { filter: AggregateType.{{findMany.TypeScriptConditionClass}}, skip?: number, take?: number }
                              // 1件編集の場合
                              | [{{keyArray.Select(k => $"{k.VarName}: {k.TsType} | undefined").Join(", ")}}]
                            ) => {
                            
                              const [, dispatchMsg] = useMsgContext()
                              const { get, post } = useHttpRequest()
                              const { ready: ready1, reload: reloadContext } = useLocalRepositoryContext()
                              const { ready: ready2, openCursor, queryToTable } = useIndexedDbLocalRepositoryTable()
                              const [ready3, setReady3] = useState(false)
                            {{refRepositories.SelectTextTemplate(x => $$"""

                              // {{x.Aggregate.Item.DisplayName}}のローカルリポジトリとリモートリポジトリへのデータ読み書き処理
                              const {{x.Aggregate.Item.ClassName}}filter: { filter: AggregateType.{{x.FindMany.TypeScriptConditionClass}} } = useMemo(() => {
                                const f = AggregateType.{{x.FindMany.TypeScriptConditionInitializerFn}}()
                                if (typeof editRange === 'string') {
                                  // 新規作成データ(未コミット)の編集の場合
                                } else if (Array.isArray(editRange)) {
                                  const [{{keyArray.Select(k => k.VarName).Join(", ")}}] = editRange
                            {{x.RootAggregateMembersForSingleViewLoading.SelectTextTemplate((kv, i) => $$"""
                            {{If(kv.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                                  f.{{kv.Declared.GetFullPath().Join(".")}}.{{FromTo.FROM}} = {{keyArray.SingleOrDefault(k => k.Member.Declared == kv.Declared)?.VarName ?? ""}}
                                  f.{{kv.Declared.GetFullPath().Join(".")}}.{{FromTo.TO}} = {{keyArray.SingleOrDefault(k => k.Member.Declared == kv.Declared)?.VarName ?? ""}}
                            """).Else(() => $$"""
                                  f.{{kv.Declared.GetFullPath().Join(".")}} = {{keyArray.SingleOrDefault(k => k.Member.Declared == kv.Declared)?.VarName ?? ""}}
                            """)}}
                            """)}}
                                } else if (editRange) {
                            {{x.RootAggregateMembersForLoad.SelectTextTemplate((kv, i) => $$"""
                            {{If(kv.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                                  f.{{kv.Declared.GetFullPath().Join(".")}}.{{FromTo.FROM}} = editRange.filter.{{FindRootAggregateSearchConditionMember(kv).GetFullPath().Join("?.")}}?.{{FromTo.FROM}}
                                  f.{{kv.Declared.GetFullPath().Join(".")}}.{{FromTo.TO}} = editRange.filter.{{FindRootAggregateSearchConditionMember(kv).GetFullPath().Join("?.")}}?.{{FromTo.TO}}
                            """).Else(() => $$"""
                                  f.{{kv.Declared.GetFullPath().Join(".")}} = editRange.filter.{{FindRootAggregateSearchConditionMember(kv).GetFullPath().Join("?.")}}
                            """)}}
                            """)}}
                                }
                                return { filter: f }
                              }, [editRange])
                              const {
                                ready: {{x.Aggregate.Item.ClassName}}IsReady,
                                items: {{x.Aggregate.Item.ClassName}}Items,
                            {{If(commitable, () => $$"""
                                commit: commit{{x.Aggregate.Item.ClassName}},
                            """)}}
                              } = {{x.Repos.HookName}}({{x.Aggregate.Item.ClassName}}filter)
                            """)}}

                              const [remoteAndLocalItems, setRemoteAndLocalItems] = useState<AggregateType.{{displayData.TsTypeName}}[]>(() => [])

                              const getItemKey = useCallback((x: AggregateType.{{agg.Item.TypeScriptTypeName}}): ItemKey => {
                                return JSON.stringify([{{keys.Select(k => $"x.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]) as ItemKey
                              }, [])
                              const getItemName = useCallback((x: AggregateType.{{displayData.TsTypeName}}) => {
                                return `{{string.Concat(names.Select(n => $"${{x.{n.Declared.GetFullPathAsSingleViewDataClass().Join("?.")}}}"))}}`
                              }, [])

                              const loadRemoteItems = useCallback(async (): Promise<AggregateType.{{displayData.TsTypeName}}[]> => {
                                if (editRange === undefined) {
                                  return [] // 画面表示直後の検索条件が決まっていない場合など

                                } else if (typeof editRange === 'string') {
                                  return [] // 新規作成データの場合、まだリモートに存在しないため検索しない

                                } else if (Array.isArray(editRange)) {
                                  if ({{keyArray.Select((_, i) => $"editRange[{i}] === undefined").Join(" || ")}}) {
                                    return []
                                  } else {
                                    const res = await get({{find.GetUrlStringForReact(keyArray.Select((_, i) => $"editRange[{i}].toString()"))}})
                                    if (!res.ok) return []
                                    const item = res.data as AggregateType.{{agg.Item.TypeScriptTypeName}}
                                    return [{{WithIndent(displayData.RenderConvertToDisplayDataClass("item"), "        ")}}]
                                  }
                                } else {
                                  const searchParam = new URLSearchParams()
                                  if (editRange.skip !== undefined) searchParam.append('{{FindManyFeature.PARAM_SKIP}}', editRange.skip.toString())
                                  if (editRange.take !== undefined) searchParam.append('{{FindManyFeature.PARAM_TAKE}}', editRange.take.toString())
                                  const url = `{{findMany.GetUrlStringForReact()}}?${searchParam}`
                                  const res = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}[]>(url, editRange.filter)
                                  if (!res.ok) return []
                                  return res.data.map(item => ({{WithIndent(displayData.RenderConvertToDisplayDataClass("item"), "      ")}}))
                                }
                              }, [editRange, getItemKey, get, post{{refRepositories.Select(x => $", {x.Aggregate.Item.ClassName}IsReady").Join("")}}])

                              const loadLocalItems = useCallback(async (): Promise<AggregateType.{{displayData.TsTypeName}}[]> => {
                                if (editRange === undefined) {
                                  return [] // 画面表示直後の検索条件が決まっていない場合など

                                } else if (typeof editRange === 'string') {
                                  // 新規作成データの検索
                                  const found = await queryToTable(table => table.get(['{{localRepositosy.DataTypeKey}}', editRange]))
                                  return found ? [found.item as AggregateType.{{displayData.TsTypeName}}] : []

                                } else if (Array.isArray(editRange)) {
                                  // 既存データのキーによる検索
                                  const itemKey = JSON.stringify(editRange)
                                  const found = await queryToTable(table => table.get(['{{localRepositosy.DataTypeKey}}', itemKey]))
                                  return found ? [found.item as AggregateType.{{displayData.TsTypeName}}] : []

                                } else {
                                  // 既存データの検索条件による検索
                                  const localItems: AggregateType.{{displayData.TsTypeName}}[] = []
                                  await openCursor('readonly', cursor => {
                                    if (cursor.value.dataTypeKey !== '{{localRepositosy.DataTypeKey}}') return
                                    // TODO: ローカルリポジトリのデータは参照先のキーと名前しか持っていないのでfilterでそれらが検索条件に含まれていると正確な範囲がとれない
                                    // const item = cursor.value.item as AggregateType.{{displayData.TsTypeName}}
                            {{findMany.EnumerateSearchConditionMembers().SelectTextTemplate(vm => $$"""
                            {{If(vm.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                                    //
                            """).Else(() => $$"""
                                    // if (editRange.filter.{{vm.Declared.GetFullPath().Join("?.")}} !== undefined
                                    //   && item.{{vm.Declared.GetFullPath().Join("?.")}} !== editRange.filter.{{vm.Declared.GetFullPath().Join(".")}}) return
                            """)}}
                            """)}}
                                    localItems.push(cursor.value.item as AggregateType.{{displayData.TsTypeName}})
                                  })
                                  return localItems
                                }
                              }, [editRange, queryToTable, openCursor])

                              const reload = useCallback(async () => {
                                if (!ready1 || !ready2) return
                                setReady3(false)
                                try {
                                  const remoteItems = await loadRemoteItems()
                                  const localItems = await loadLocalItems()
                                  const remoteAndLocal = crossJoin(
                                    localItems, local => local.{{DisplayDataClass.LOCAL_REPOS_ITEMKEY}},
                                    remoteItems, remote => remote.{{DisplayDataClass.LOCAL_REPOS_ITEMKEY}},
                                  ).map<AggregateType.{{displayData.TsTypeName}}>(pair => pair.left ?? pair.right)
                                  setRemoteAndLocalItems(remoteAndLocal)

                                } finally {
                                  setReady3(true)
                                }
                              }, [ready1, ready2, loadRemoteItems, loadLocalItems, getItemKey])

                              useEffect(() => {
                                reload()
                              }, [reload])

                            {{If(commitable, () => $$"""
                              /** 引数に渡されたデータをローカルリポジトリに登録します。 */
                              const commit = useCallback(async (...items: AggregateType.{{displayData.TsTypeName}}[]): Promise<AggregateType.{{displayData.TsTypeName}}[]> => {
                                const result: AggregateType.{{displayData.TsTypeName}}[] = []

                                for (const newValue of items) {
                                  if (newValue.willBeDeleted && !newValue.existsInRemoteRepository) {
                                    await queryToTable(table => table.delete(['{{localRepositosy.DataTypeKey}}', newValue.{{DisplayDataClass.LOCAL_REPOS_ITEMKEY}}]))

                                  } else if (newValue.willBeChanged || newValue.willBeDeleted) {
                                    await queryToTable(table => table.put({
                                      dataTypeKey: '{{localRepositosy.DataTypeKey}}',
                                      itemName: getItemName?.(newValue) ?? '',
                                      itemKey: newValue.{{DisplayDataClass.LOCAL_REPOS_ITEMKEY}},
                                      item: newValue,
                                      existsInRemoteRepository: newValue.{{DisplayDataClass.EXISTS_IN_REMOTE_REPOS}},
                                      willBeChanged: newValue.{{DisplayDataClass.WILL_BE_CHANGED}},
                                      willBeDeleted: newValue.{{DisplayDataClass.WILL_BE_DELETED}},
                                    }))
                                    result.push(newValue)

                                  } else {
                                    result.push(newValue)
                                  }
                                }
                                await reloadContext()
                                return result
                              }, [reloadContext, getItemName, queryToTable])

                            """)}}
                              return {
                                ready: ready1 && ready2 && ready3,
                                items: remoteAndLocalItems,
                                reload,
                            {{If(commitable, () => $$"""
                                commit,
                            """)}}
                              }
                            }
                            """;
                    });

                    return $$"""
                        import { useState, useMemo, useCallback, useEffect } from 'react'
                        import { UUID } from 'uuidjs'
                        import { useMsgContext } from './Notification'
                        import { useHttpRequest } from './Http'
                        import {
                          ItemKey,
                          useLocalRepositoryContext,
                          useIndexedDbLocalRepositoryTable,
                        } from './LocalRepository'
                        import { crossJoin } from './JsUtil'
                        import * as AggregateType from '../autogenerated-types'

                        {{localReposWrapperHooks}}
                        """;
                },
            };
        }


        internal static SourceFile UseLocalRepositoryCommitHandling(CodeRenderingContext context) {

            var aggregates = context.Schema
                .RootAggregatesOrderByDataFlow()
                .Where(agg => agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);

            static string RenderCommitFunction(GraphNode<Aggregate> agg) {
                var displayData = new DisplayDataClass(agg);
                var localRepos = new LocalRepository(agg);
                var controller = new Parts.WebClient.Controller(agg.Item);
                var deleteKeyUrlParam = agg
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(vm => $"${{saveItem.{vm.Declared.GetFullPath().Join("?.")}}}")
                    .Join("/");

                return $$"""
                    async (localReposItem: LocalRepositoryStoredItem<AggregateType.{{displayData.TsTypeName}}>) => {
                      const [{ item: saveItem }] = AggregateType.{{displayData.ConvertFnNameToLocalRepositoryType}}(localReposItem.item)
                      const state = getLocalRepositoryState(localReposItem)
                      if (state === '+') {
                        const url = `{{controller.CreateCommandApi}}`
                        const response = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}>(url, saveItem)
                        return { commit: response.ok }

                      } else if (state === '*') {
                        const url = `{{controller.UpdateCommandApi}}`
                        const response = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}>(url, saveItem)
                        return { commit: response.ok }
                    
                      } else if (state === '-') {
                        const url = `{{controller.DeleteCommandApi}}/{{deleteKeyUrlParam}}`
                        const response = await httpDelete(url)
                        return { commit: response.ok }
                    
                      } else {
                        dispatchMsg(msg => msg.error(`'${saveItem}' の状態 '${state}' が不正です。`))
                        return { commit: false }
                      }
                    }
                    """;
            }

            return new SourceFile {
                FileName = "LocalRepository.Commit.ts",
                RenderContent = context => $$"""
                    /**
                     * このファイルはソース自動生成によって上書きされます。
                     */
                    import { useCallback, useMemo } from 'react'
                    import { useMsgContext } from './Notification'
                    import { useHttpRequest } from './Http'
                    import { ItemKey, LocalRepositoryContextValue, LocalRepositoryStoredItem, SaveLocalItemHandler, getLocalRepositoryState } from './LocalRepository'
                    import * as AggregateType from '../autogenerated-types'

                    export const useLocalRepositoryCommitHandling = () => {
                      const [, dispatchMsg] = useMsgContext()
                      const { post, httpDelete } = useHttpRequest()

                      const saveHandlerMap = useMemo(() => new Map<string, SaveLocalItemHandler<any>>([
                    {{aggregates.SelectTextTemplate(agg => $$"""
                        ['{{new LocalRepository(agg).DataTypeKey}}', {{WithIndent(RenderCommitFunction(agg), "    ")}}],
                    """)}}
                      ]), [post, httpDelete, dispatchMsg])

                      return useCallback(async (
                        commit: LocalRepositoryContextValue['commit'],
                        ...keys: { dataTypeKey: string, itemKey: ItemKey }[]
                      ) => {
                        const success = await commit(async localReposItem => {
                          const handler = saveHandlerMap.get(localReposItem.dataTypeKey)
                          if (!handler) {
                            dispatchMsg(msg => msg.error(`データ型 '${localReposItem.dataTypeKey}' の保存処理が定義されていません。`))
                            return { commit: false }
                          }
                          return await handler(localReposItem)
                        }, ...keys)

                        dispatchMsg(msg => success
                          ? msg.info('保存しました。')
                          : msg.info('一部のデータの保存に失敗しました。'))

                      }, [saveHandlerMap, dispatchMsg])
                    }
                    """,
            };
        }
    }
}
