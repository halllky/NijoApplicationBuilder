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
                                    return [AggregateType.{{displayData.ConvertFnNameToDisplayDataType}}({
                                      item,
                                      itemKey: getItemKey(item),
                                      existsInRemoteRepository: true,
                                      willBeChanged: false,
                                      willBeDeleted: false,
                                    })]
                                  }
                                } else {
                                  const searchParam = new URLSearchParams()
                                  if (editRange.skip !== undefined) searchParam.append('{{FindManyFeature.PARAM_SKIP}}', editRange.skip.toString())
                                  if (editRange.take !== undefined) searchParam.append('{{FindManyFeature.PARAM_TAKE}}', editRange.take.toString())
                                  const url = `{{findMany.GetUrlStringForReact()}}?${searchParam}`
                                  const res = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}[]>(url, editRange.filter)
                                  if (!res.ok) return []
                                  return res.data.map(item => AggregateType.{{displayData.ConvertFnNameToDisplayDataType}}({
                                    item,
                                    itemKey: getItemKey(item),
                                    existsInRemoteRepository: true,
                                    willBeChanged: false,
                                    willBeDeleted: false,
                                  }))
                                }
                              }, [editRange, getItemKey, get, post])

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
