using Nijo.Core;
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
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        /// <summary>
        /// ローカルリポジトリ内にあるデータそれぞれに割り当てられる、そのデータの種類が何かを識別する文字列
        /// </summary>
        internal string DataTypeKey => _aggregate.Item.ClassName;
        internal string HookName => $"use{_aggregate.Item.ClassName}Repository";

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
                        .Where(agg => agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);
                    var hooks = aggregates.SelectTextTemplate(agg => {
                        var localRepositosy = new LocalRepository(agg);
                        var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                        var names = agg.GetNames().OfType<AggregateMember.ValueMember>();
                        var keyArray = KeyArray.Create(agg);
                        var find = new FindFeature(agg);
                        var findMany = new FindManyFeature(agg);

                        return $$"""
                            export const {{localRepositosy.HookName}} = (editRange?
                              // 複数件編集の場合
                              : { filter: AggregateType.{{findMany.TypeScriptConditionClass}}, skip?: number, take?: number }
                              // 1件編集の場合
                              | [{{keyArray.Select(k => $"{k.VarName}: {k.TsType} | undefined").Join(", ")}}]
                            ) => {
                              const [remoteItems, setRemoteItems] = useState<AggregateType.{{agg.Item.TypeScriptTypeName}}[]>(() => [])
                              const [remoteAndLocalItems, setRemoteAndLocalItems] = useState<LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[]>(() => [])
                              const { get, post } = useHttpRequest()
                              const [ready, setReady] = useState(false)

                              // ローカルリポジトリ設定
                              const localReposSetting: LocalRepositoryArgs<AggregateType.{{agg.Item.TypeScriptTypeName}}> = useMemo(() => ({
                                dataTypeKey: '{{localRepositosy.DataTypeKey}}',
                                getItemKey: x => JSON.stringify([{{keys.Select(k => $"x.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]),
                                getItemName: x => `{{string.Concat(names.Select(n => $"${{x.{n.Declared.GetFullPath().Join("?.")}}}"))}}`,
                                remoteItems,
                              }), [remoteItems])
                              const {
                                ready: localRepositoryIsReady,
                                loadLocalItems,
                                addToLocalRepository: add,
                                updateLocalRepositoryItem: update,
                                deleteLocalRepositoryItem: remove,
                              } = useLocalRepository(localReposSetting)

                              // データ読み込み & 保持している状態の更新
                              const reload = useCallback(async () => {
                                setReady(false)
                                if (!localRepositoryIsReady) return
                                if (editRange === undefined) return // 画面表示直後の検索条件が決まっていない場合など

                                // リモートから読み込む
                                let remote: AggregateType.{{agg.Item.TypeScriptTypeName}}[]
                                if (Array.isArray(editRange)) {
                            {{keyArray.SelectTextTemplate((_, i) => $$"""
                                  if (editRange[{{i}}] === undefined) return
                            """)}}
                                  const res = await get({{find.GetUrlStringForReact(keyArray.Select((_, i) => $"editRange[{i}].toString()"))}})
                                  remote = res.ok ? [res.data as AggregateType.{{agg.Item.TypeScriptTypeName}}] : []
                                } else {
                                  const searchParam = new URLSearchParams()
                                  if (editRange.skip !== undefined) searchParam.append('{{FindManyFeature.PARAM_SKIP}}', editRange.skip.toString())
                                  if (editRange.take !== undefined) searchParam.append('{{FindManyFeature.PARAM_TAKE}}', editRange.take.toString())
                                  const url = `{{findMany.GetUrlStringForReact()}}?${searchParam}`
                                  const res = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}[]>(url, editRange.filter)
                                  remote = res.ok ? res.data : []
                                }

                                // ローカルから読み込んでリモートから読み込んだものと比較する
                                const remoteAndLocal = await loadLocalItems(remote)

                                // UIの都合による属性設定
                                for (let item of remoteAndLocal.map(x => x.item)) {
                                  Util.visitObject(item, obj => {
                                    // 新規データのみ主キーを編集可能にするため、読込データと新規データを区別するためのフラグをつける
                                    (obj as { {{DisplayDataClass.IS_LOADED}}?: boolean }).{{DisplayDataClass.IS_LOADED}} = true;
                                    // 配列中のオブジェクト識別用
                                    (obj as { {{DisplayDataClass.OBJECT_ID}}: string }).{{DisplayDataClass.OBJECT_ID}} = UUID.generate()
                                  })
                                }

                                // 状態更新
                                setRemoteAndLocalItems(remoteAndLocal)
                                setReady(true)
                                setRemoteItems(remote) // addやremoveのタイミングでリモートにデータがあるかどうかの情報が必要になるので
                                return remoteAndLocal
                              }, [editRange, localRepositoryIsReady, loadLocalItems])

                              useEffect(() => {
                                reload()
                              }, [reload])

                              return { ready, items: remoteAndLocalItems, reload, add, update, remove }
                            }
                            """;
                    });

                    return $$"""
                        import { useState, useMemo, useCallback, useEffect } from 'react'
                        import { useHttpRequest } from './Http'
                        import { useLocalRepository, LocalRepositoryArgs, LocalRepositoryItem } from './LocalRepository'
                        import * as AggregateType from '../autogenerated-types'

                        {{hooks}}
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
                    import { ItemKey, LocalRepositoryContextValue, LocalRepositoryStoredItem, SaveLocalItemHandler } from './LocalRepository'
                    import * as AggregateType from '../autogenerated-types'

                    export const useLocalRepositoryCommitHandling = () => {
                      const [, dispatchMsg] = useMsgContext()
                      const { post, httpDelete } = useHttpRequest()

                      const saveHandlerMap = useMemo(() => new Map<string, SaveLocalItemHandler>([
                    {{aggregates.SelectTextTemplate(agg => $$"""
                        ['{{new LocalRepository(agg).DataTypeKey}}', async (localReposItem: LocalRepositoryStoredItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>) => {
                          if (localReposItem.state === '+') {
                            const url = `{{new Parts.WebClient.Controller(agg.Item).CreateCommandApi}}`
                            const response = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}>(url, localReposItem.item)
                            return { commit: response.ok }

                          } else if (localReposItem.state === '*') {
                            const url = `{{new Parts.WebClient.Controller(agg.Item).UpdateCommandApi}}`
                            const response = await post<AggregateType.{{agg.Item.TypeScriptTypeName}}>(url, localReposItem.item)
                            return { commit: response.ok }
                    
                          } else if (localReposItem.state === '-') {
                            const url = `{{new Parts.WebClient.Controller(agg.Item).DeleteCommandApi}}/{{DeleteKeyUrlParam(agg)}}`
                            const response = await httpDelete(url)
                            return { commit: response.ok }
                    
                          } else {
                            dispatchMsg(msg => msg.error(`'${localReposItem.itemKey}' の状態 '${localReposItem.state}' が不正です。`))
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
