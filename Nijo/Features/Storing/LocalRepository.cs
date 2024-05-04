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
        /// <see cref="TransactionScopeDataClass"/> と <see cref="DisplayDataClass"/> の変換を行うフック
        /// </summary>
        internal string HookName => $"use{Aggregate.Item.ClassName}Repository";
        /// <summary>
        /// useLocalRepository フックのラッパー
        /// </summary>
        private string LocalLoaderHookName => $"use{Aggregate.Item.ClassName}LocalRepository";

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

                    var convesionBetweenDisplayDataAndTranScopeDataHooks = aggregates.SelectTextTemplate(agg => {
                        var dataClass = new DisplayDataClass(agg);
                        var localRepositosy = new LocalRepository(agg);
                        var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                        var names = agg.GetNames().OfType<AggregateMember.ValueMember>();
                        var keyArray = KeyArray.Create(agg);
                        var find = new FindFeature(agg);
                        var findMany = new FindManyFeature(agg);

                        var commitable = agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key;

                        return $$"""
                            /** {{agg.Item.DisplayName}}の画面に表示するデータ型と登録更新するデータ型の変換を行うフック */
                            export const {{localRepositosy.HookName}} = (editRange?
                              // データ新規作成の場合
                              : ItemKey
                              // 複数件編集の場合
                              | { filter: AggregateType.{{findMany.TypeScriptConditionClass}}, skip?: number, take?: number }
                              // 1件編集の場合
                              | [{{keyArray.Select(k => $"{k.VarName}: {k.TsType} | undefined").Join(", ")}}]
                            ) => {

                              // {{agg.Item.DisplayName}}のローカルリポジトリとリモートリポジトリへのデータ読み書き処理
                              const {
                                ready,
                                items: {{agg.Item.ClassName}}Items,
                            {{If(commitable, () => $$"""
                                commit: commit{{agg.Item.ClassName}},
                            """)}}
                                reload,
                              } = {{localRepositosy.LocalLoaderHookName}}(editRange)

                              // 登録更新のデータ型を画面表示用のデータ型に変換する
                              const [items, setItems] = useState<AggregateType.{{dataClass.TsTypeName}}[]>(() => [])
                              const allReady = ready
                              useEffect(() => {
                                if (allReady) {
                                  const currentPageItems: AggregateType.{{dataClass.TsTypeName}}[] = {{agg.Item.ClassName}}Items.map(item => {
                                    return AggregateType.{{dataClass.ConvertFnNameToDisplayDataType}}(item)
                                  })
                                  setItems(currentPageItems)
                                }
                              }, [allReady, {{agg.Item.ClassName}}Items])

                            {{If(commitable, () => $$"""
                              // 保存
                              const commit = useCallback(async (...commitItems: AggregateType.{{dataClass.TsTypeName}}[]) => {

                                // 画面表示用のデータ型を登録更新のデータ型に変換する
                                const arr{{agg.Item.ClassName}}: LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[] = []
                                for (const item of commitItems) {
                                  const [
                                    item{{agg.Item.ClassName}}
                                  ] = AggregateType.{{dataClass.ConvertFnNameToLocalRepositoryType}}(item)

                                  arr{{agg.Item.ClassName}}.push(item{{agg.Item.ClassName}})
                                }

                                // リポジトリへ反映する
                                await commit{{agg.Item.ClassName}}(...arr{{agg.Item.ClassName}})
                              }, [commit{{agg.Item.ClassName}}])

                            """)}}
                              return { ready: allReady, items{{(commitable ? ", commit" : "")}}, reload }
                            }
                            """;
                    });

                    var localReposWrapperHooks = aggregates.SelectTextTemplate(agg => {
                        var localRepositosy = new LocalRepository(agg);
                        var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                        var names = agg.GetNames().OfType<AggregateMember.ValueMember>();
                        var keyArray = KeyArray.Create(agg);
                        var find = new FindFeature(agg);
                        var findMany = new FindManyFeature(agg);

                        var commitable = agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key;

                        return $$"""
                            const {{localRepositosy.LocalLoaderHookName}} = (editRange?
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

                              const [remoteAndLocalItems, setRemoteAndLocalItems] = useState<LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[]>(() => [])

                              const getItemKey = useCallback((x: AggregateType.{{agg.Item.TypeScriptTypeName}}): ItemKey => {
                                return JSON.stringify([{{keys.Select(k => $"x.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]) as ItemKey
                              }, [])
                              const getItemName = useCallback((x: AggregateType.{{agg.Item.TypeScriptTypeName}}) => {
                                return `{{string.Concat(names.Select(n => $"${{x.{n.Declared.GetFullPath().Join("?.")}}}"))}}`
                              }, [])

                              const loadRemoteItems = useCallback(async (): Promise<AggregateType.{{agg.Item.TypeScriptTypeName}}[]> => {
                                if (editRange === undefined) {
                                  return [] // 画面表示直後の検索条件が決まっていない場合など

                                } else if (typeof editRange === 'string') {
                                  return [] // 新規作成データの場合、まだリモートに存在しないため検索しない

                                } else if (Array.isArray(editRange)) {
                                  if ({{keyArray.Select((_, i) => $"editRange[{i}] === undefined").Join(" || ")}}) {
                                    return []
                                  } else {
                                    const res = await get({{find.GetUrlStringForReact(keyArray.Select((_, i) => $"editRange[{i}].toString()"))}})
                                    return res.ok ? [res.data as AggregateType.{{agg.Item.TypeScriptTypeName}}] : []
                                  }
                                } else {
                                  const searchParam = new URLSearchParams()
                                  if (editRange.skip !== undefined) searchParam.append('{{FindManyFeature.PARAM_SKIP}}', editRange.skip.toString())
                                  if (editRange.take !== undefined) searchParam.append('{{FindManyFeature.PARAM_TAKE}}', editRange.take.toString())
                                  const url = `{{findMany.GetUrlStringForReact()}}?${searchParam}`
                                  const res = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}[]>(url, editRange.filter)
                                  return res.ok ? res.data : []
                                }
                              }, [editRange, get, post])

                              const loadLocalItems = useCallback(async (): Promise<LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[]> => {
                                if (editRange === undefined) {
                                  return [] // 画面表示直後の検索条件が決まっていない場合など

                                } else if (typeof editRange === 'string') {
                                  // 新規作成データの検索
                                  const found = await queryToTable(table => table.get(['{{localRepositosy.DataTypeKey}}', editRange]))
                                  return found ? [found] : []

                                } else if (Array.isArray(editRange)) {
                                  // 既存データのキーによる検索
                                  const itemKey = JSON.stringify(editRange)
                                  const found = await queryToTable(table => table.get(['{{localRepositosy.DataTypeKey}}', itemKey]))
                                  return found ? [found] : []

                                } else {
                                  // 既存データの検索条件による検索
                                  const localItems: LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[] = []
                                  await openCursor('readonly', cursor => {
                                    if (cursor.value.dataTypeKey !== '{{localRepositosy.DataTypeKey}}') return
                                    // TODO: ローカルリポジトリのデータは参照先のキーと名前しか持っていないのでfilterでそれらが検索条件に含まれていると正確な範囲がとれない
                                    // const item = cursor.value.item as AggregateType.{{agg.Item.TypeScriptTypeName}}
                            {{findMany.EnumerateSearchConditionMembers().SelectTextTemplate(vm => $$"""
                            {{If(vm.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                                    //
                            """).Else(() => $$"""
                                    // if (editRange.filter.{{vm.Declared.GetFullPath().Join("?.")}} !== undefined
                                    //   && item.{{vm.Declared.GetFullPath().Join("?.")}} !== editRange.filter.{{vm.Declared.GetFullPath().Join(".")}}) return
                            """)}}
                            """)}}
                                    localItems.push({ ...cursor.value, item: cursor.value.item as AggregateType.{{agg.Item.TypeScriptTypeName}} })
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
                                    localItems, local => local.itemKey,
                                    remoteItems, remote => getItemKey(remote) as ItemKey,
                                  ).map<LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>>(pair => pair.left ?? ({
                                    itemKey: pair.key,
                                    item: pair.right,
                                    existsInRemoteRepository: true,
                                    willBeChanged: false,
                                    willBeDeleted: false,
                                  }))
                                  setRemoteAndLocalItems(remoteAndLocal)

                                } finally {
                                  setReady3(true)
                                }
                              }, [ready1, ready2, loadRemoteItems, loadLocalItems, getItemKey])

                              useEffect(() => {
                                reload()
                              }, [reload])

                            {{If(commitable, () => $$"""
                              /** 引数に渡されたデータの値を見てstateを適切に変更し然るべき場所への保存を判断し実行する。 */
                              const commit = useCallback(async (...items: LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[]): Promise<LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[]> => {
                                const remoteItems = await loadRemoteItems()
                                const localItems = await loadLocalItems()
                                const remoteAndLocalTemp = crossJoin(
                                  localItems, local => local.itemKey,
                                  remoteItems, remote => getItemKey(remote) as ItemKey,
                                ).map<readonly [ItemKey, LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>]>(pair => [
                                  pair.key,
                                  pair.left ?? ({
                                    itemKey: pair.key,
                                    item: pair.right,
                                    existsInRemoteRepository: true,
                                    willBeChanged: false,
                                    willBeDeleted: false,
                                  })
                                ] as const)
                                const remoteAndLocal = new Map(remoteAndLocalTemp)

                                // -------------------------
                                // 状態更新。UIで不具合が起きる可能性を考慮し、とにかくエラーチェックを厳格に作る
                                const result: LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[] = []
                                for (const newValue of items) {
                                  const stored = remoteAndLocal.get(newValue.itemKey)

                                  // バリデーション
                                  // TODO: 楽観排他制御の考慮が無い
                                  if (!newValue.existsInRemoteRepository && stored?.existsInRemoteRepository) {
                                    dispatchMsg(msg => msg.warn(`既に存在するデータと同じキーで新規追加しようとしました: ${newValue.itemKey}`))
                                    result.push(newValue)
                                    continue
                                  }
                                  if (newValue.existsInRemoteRepository && !stored?.existsInRemoteRepository) {
                                    dispatchMsg(msg => msg.warn(`更新対象データがリモートにもローカルにもありません: ${newValue.itemKey}`))
                                    result.push(newValue)
                                    continue
                                  }

                                  // ローカルリポジトリの更新
                                  if (newValue.willBeDeleted && !newValue.existsInRemoteRepository) {
                                    await queryToTable(table => table.delete(['{{localRepositosy.DataTypeKey}}', newValue.itemKey]))

                                  } else if (newValue.willBeChanged || newValue.willBeDeleted) {
                                    const itemName = getItemName?.(newValue.item) ?? ''
                                    await queryToTable(table => table.put({ dataTypeKey: '{{localRepositosy.DataTypeKey}}', itemName, ...newValue }))
                                    result.push(newValue)

                                  } else {
                                    result.push(newValue)
                                  }
                                }
                                await reloadContext()
                                return result
                              }, [loadRemoteItems, loadLocalItems, reloadContext, dispatchMsg, getItemKey, getItemName, queryToTable])

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
                          LocalRepositoryItem,
                          useLocalRepositoryContext,
                          useIndexedDbLocalRepositoryTable,
                        } from './LocalRepository'
                        import { crossJoin } from './JsUtil'
                        import * as AggregateType from '../autogenerated-types'

                        {{convesionBetweenDisplayDataAndTranScopeDataHooks}}

                        // -----------------------------------------------------------

                        {{localReposWrapperHooks}}
                        """;
                },
            };
        }


        internal static SourceFile UseLocalRepositoryCommitHandling(CodeRenderingContext context) {

            var aggregates = context.Schema
                .RootAggregatesOrderByDataFlow()
                .Where(agg => agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);

            static string DeleteKeyUrlParam(GraphNode<Aggregate> agg) {
                return agg
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(vm => $"${{localReposItem.item.{vm.Declared.GetFullPath().Join("?.")}}}")
                    .Join("/");
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

                      const saveHandlerMap = useMemo(() => new Map<string, SaveLocalItemHandler>([
                    {{aggregates.SelectTextTemplate(agg => $$"""
                        ['{{new LocalRepository(agg).DataTypeKey}}', async (localReposItem: LocalRepositoryStoredItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>) => {
                          const state = getLocalRepositoryState(localReposItem)
                          if (state === '+') {
                            const url = `{{new Parts.WebClient.Controller(agg.Item).CreateCommandApi}}`
                            const response = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}>(url, localReposItem.item)
                            return { commit: response.ok }

                          } else if (state === '*') {
                            const url = `{{new Parts.WebClient.Controller(agg.Item).UpdateCommandApi}}`
                            const response = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}>(url, localReposItem.item)
                            return { commit: response.ok }
                    
                          } else if (state === '-') {
                            const url = `{{new Parts.WebClient.Controller(agg.Item).DeleteCommandApi}}/{{DeleteKeyUrlParam(agg)}}`
                            const response = await httpDelete(url)
                            return { commit: response.ok }
                    
                          } else {
                            dispatchMsg(msg => msg.error(`'${localReposItem.itemKey}' の状態 '${state}' が不正です。`))
                            return { commit: false }
                          }
                        }],
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
