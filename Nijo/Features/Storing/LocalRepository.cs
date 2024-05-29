using Nijo.Core;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
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
        internal string DataTypeKey => Aggregate.Item.PhysicalName;
        /// <summary>
        /// 永続化層の抽象のフック。外部とのインターフェースの型は <see cref="DataClassForDisplay"/>
        /// </summary>
        internal string HookName => $"use{Aggregate.Item.PhysicalName}Repository";

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
                        var displayData = new DataClassForDisplay(agg);
                        var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                        // TODO 参照先の名前の表示処理をちゃんとする
                        var names = agg.GetNames().OfType<AggregateMember.ValueMember>().Where(x => x.DeclaringAggregate == agg);
                        var keyArray = KeyArray.Create(agg);
                        var find = new FindFeature(agg);
                        var findMany = new FindManyFeature(agg);

                        var commitable = agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key;

                        var refRepositories = agg
                            .EnumerateThisAndDescendants()
                            .Select(a => new DataClassForDisplay(a))
                            .SelectMany(
                                a =>  a.GetRefFromProps(),
                                (x, refFromProp) => new { DisplayData = x, RefFrom = refFromProp })
                            .Select(x => new {
                                RefTo = x.DisplayData,
                                x.RefFrom,
                                Repos = new LocalRepository(x.RefFrom.MainAggregate),
                                FindMany = new FindManyFeature(x.RefFrom.MainAggregate),

                                // この画面のメイン集約を参照する関連集約をまとめて読み込むため、
                                // SingleViewのURLのキーで関連集約のAPIへの検索をかけたい。
                                // そのために当該検索条件のうち関連集約の検索に関係するメンバーの一覧
                                RootAggregateMembersForSingleViewLoading = x.RefFrom.MainAggregate
                                    .GetEntryReversing()
                                    .As<Aggregate>()
                                    .GetMembers()
                                    .OfType<AggregateMember.ValueMember>()
                                    .Where(vm => keyArray.Any(k => k.Member.Declared == vm.Declared)),

                                // この画面のメイン集約を参照する関連集約をまとめて読み込むため、
                                // MultiViewの画面上部の検索条件の値で関連集約のAPIへの検索をかけたい。
                                // そのために当該検索条件のうち関連集約の検索に関係するメンバーの一覧
                                RootAggregateMembersForLoad = x.RefFrom.MainAggregate
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

                        static string RenderSelectMany(GraphNode<Aggregate> agg) {
                            var builder = new StringBuilder();
                            var array = false;
                            foreach (var e in agg.PathFromEntry()) {

                                // 念のため
                                if (!e.IsParentChild()) throw new InvalidOperationException("このメソッドには同一ツリー内の集約しか来ないはず");

                                var edge = e.As<Aggregate>();
                                var prop = new DataClassForDisplay(edge.Initial)
                                    .GetChildProps()
                                    .Single(p => p.MainAggregate == edge.Terminal);

                                if (array && edge.Terminal.IsChildrenMember()) {
                                    builder.Append($"?.flatMap(x => x.{prop.PropName} ?? [])");
                                } else if (array) {
                                    builder.Append($"?.map(x => x.{prop.PropName})");
                                } else {
                                    builder.Append($"?.{prop.PropName}");
                                    if (edge.Terminal.IsChildrenMember()) array = true;
                                }
                            }
                            return builder.ToString();
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
                              const { reload: reloadContext } = useLocalRepositoryContext()
                              const { ready: ready2, openCursor, queryToTable } = useIndexedDbLocalRepositoryTable()
                            {{refRepositories.SelectTextTemplate(x => $$"""

                              // {{x.RefFrom.MainAggregate.Item.DisplayName}}のローカルリポジトリとリモートリポジトリへのデータ読み書き処理
                              const {{x.RefFrom.MainAggregate.Item.PhysicalName}}filter: { filter: AggregateType.{{x.FindMany.TypeScriptConditionClass}} } = useMemo(() => {
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
                                ready: {{x.RefFrom.MainAggregate.Item.PhysicalName}}IsReady,
                                load: load{{x.RefFrom.MainAggregate.Item.PhysicalName}},
                            {{If(commitable, () => $$"""
                                commit: commit{{x.RefFrom.MainAggregate.Item.PhysicalName}},
                            """)}}
                              } = {{x.Repos.HookName}}({{x.RefFrom.MainAggregate.Item.PhysicalName}}filter)
                            """)}}

                              const load = useCallback(async (): Promise<AggregateType.{{displayData.TsTypeName}}[] | undefined> => {
                                if (!ready2) return
                                if (editRange === undefined) return // 画面表示直後の検索条件が決まっていない場合など

                            {{refRepositories.SelectTextTemplate(x => $$"""
                                const loaded{{x.RefFrom.MainAggregate.Item.PhysicalName}} = await load{{x.RefFrom.MainAggregate.Item.PhysicalName}}()
                                if (!loaded{{x.RefFrom.MainAggregate.Item.PhysicalName}}) return // {{x.RefFrom.MainAggregate.Item.DisplayName}}の読み込み完了まで待機
                            """)}}

                                let remoteItems: AggregateType.{{new DataClassForDisplay(agg).TsTypeName}}[]
                                let localItems: AggregateType.{{displayData.TsTypeName}}[]

                                if (typeof editRange === 'string') {
                                  // 新規作成データの検索。
                                  // まだリモートに存在しないためローカルにのみ検索をかける
                                  remoteItems = []
                                  const found = await queryToTable(table => table.get(['{{localRepositosy.DataTypeKey}}', editRange]))
                                  localItems = found ? [found.item as AggregateType.{{displayData.TsTypeName}}] : []

                                } else if (Array.isArray(editRange)) {
                                  // 既存データのキーによる検索（リモートリポジトリ）
                                  if ({{keyArray.Select((_, i) => $"editRange[{i}] === undefined").Join(" || ")}}) {
                                    remoteItems = []
                                  } else {
                                    const res = await get({{find.GetUrlStringForReact(keyArray.Select((_, i) => $"editRange[{i}].toString()"))}})
                                    remoteItems = res.ok
                                      ? [res.data as AggregateType.{{new DataClassForDisplay(agg).TsTypeName}}]
                                      : []
                                  }

                                  // 既存データのキーによる検索（ローカルリポジトリ）
                                  const itemKey = JSON.stringify(editRange)
                                  const found = await queryToTable(table => table.get(['{{localRepositosy.DataTypeKey}}', itemKey]))
                                  localItems = found ? [found.item as AggregateType.{{displayData.TsTypeName}}] : []

                                } else {
                                  // 既存データの検索条件による検索（リモートリポジトリ）
                                  const searchParam = new URLSearchParams()
                                  if (editRange.skip !== undefined) searchParam.append('{{FindManyFeature.PARAM_SKIP}}', editRange.skip.toString())
                                  if (editRange.take !== undefined) searchParam.append('{{FindManyFeature.PARAM_TAKE}}', editRange.take.toString())
                                  const url = `{{findMany.GetUrlStringForReact()}}?${searchParam}`
                                  const res = await post<AggregateType.{{new DataClassForDisplay(agg).TsTypeName}}[]>(url, editRange.filter)
                                  remoteItems = res.ok ? res.data : []

                                  // 既存データの検索条件による検索（ローカルリポジトリ）
                                  localItems = []
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
                                }

                                // ローカルリポジトリにあるデータはそちらを優先的に表示する
                                const remoteAndLocal =  crossJoin(
                                  localItems, local => local.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}},
                                  remoteItems, remote => remote.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}},
                                ).map<AggregateType.{{displayData.TsTypeName}}>(pair => pair.left ?? pair.right)
                            {{refRepositories.SelectTextTemplate(x => $$"""

                                // {{new DataClassForSave(x.RefFrom.MainAggregate).TsTypeName}}を{{new DataClassForSave(agg).TsTypeName}}に合成する
                                for (const item of remoteAndLocal) {
                            {{If(x.RefTo.MainAggregate.EnumerateAncestorsAndThis().Any(y => y.IsChildrenMember()), () => $$"""
                                  for (const x of item{{RenderSelectMany(x.RefTo.MainAggregate)}} ?? []) {
                                    x.{{x.RefFrom.PropName}} = loaded{{x.RefFrom.MainAggregate.Item.PhysicalName}}.find(y => y.{{x.RefFrom.MainAggregate.AsEntry().GetSingleRefKeyAggregate()?.GetFullPathAsSingleViewDataClass().Join(".")}} === x.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}})
                                  }
                            """).Else(() => $$"""
                                  item.{{x.RefFrom.PropName}} = loaded{{x.RefFrom.MainAggregate.Item.PhysicalName}}.find(y => y.{{x.RefFrom.MainAggregate.AsEntry().GetSingleRefKeyAggregate()?.GetFullPathAsSingleViewDataClass().Join(".")}} === item.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}})
                            """)}}
                                }
                            """)}}

                                return remoteAndLocal

                              }, [editRange, get, post, queryToTable, openCursor{{refRepositories.Select(x => $", load{x.RefFrom.MainAggregate.Item.PhysicalName}").Join("")}}])

                            {{If(commitable, () => $$"""
                              /** 引数に渡されたデータをローカルリポジトリに登録します。 */
                              const commit = useCallback(async (...items: AggregateType.{{displayData.TsTypeName}}[]) => {
                                for (const newValue of items) {
                            {{refRepositories.SelectTextTemplate(x => $$"""
                            {{If(x.RefTo.MainAggregate.EnumerateAncestorsAndThis().Any(y => y.IsChildrenMember()), () => $$"""
                                  const arr{{x.RefFrom.MainAggregate.Item.PhysicalName}}: AggregateType.{{x.RefFrom.TsTypeName}}[] = []
                                  for (const x of newValue{{RenderSelectMany(x.RefTo.MainAggregate)}} ?? []) {
                                    if (x.{{x.RefFrom.PropName}} === undefined) continue
                                    arr{{x.RefFrom.MainAggregate.Item.PhysicalName}}.push(x.{{x.RefFrom.PropName}})
                                    delete x.{{x.RefFrom.PropName}}
                                  }
                                  await commit{{x.RefFrom.MainAggregate.Item.PhysicalName}}(...arr{{x.RefFrom.MainAggregate.Item.PhysicalName}})

                            """).Else(() => $$"""
                                  if (newValue.{{x.RefFrom.PropName}}) {
                                    await commit{{x.RefFrom.MainAggregate.Item.PhysicalName}}(newValue.{{x.RefFrom.PropName}})
                                    delete newValue.{{x.RefFrom.PropName}}
                                  }

                            """)}}
                            """)}}
                                  if (newValue.willBeDeleted && !newValue.existsInRemoteRepository) {
                                    await queryToTable(table => table.delete(['{{localRepositosy.DataTypeKey}}', newValue.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}}]))

                                  } else if (newValue.willBeChanged || newValue.willBeDeleted) {
                                    await queryToTable(table => table.put({
                                      dataTypeKey: '{{localRepositosy.DataTypeKey}}',
                                      itemKey: newValue.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}},
                                      itemName: `{{string.Concat(names.Select(n => $"${{newValue.{n.Declared.GetFullPathAsSingleViewDataClass().Join("?.")}}}"))}}`,
                                      item: newValue,
                                      existsInRemoteRepository: newValue.{{DataClassForDisplay.EXISTS_IN_REMOTE_REPOS}},
                                      willBeChanged: newValue.{{DataClassForDisplay.WILL_BE_CHANGED}},
                                      willBeDeleted: newValue.{{DataClassForDisplay.WILL_BE_DELETED}},
                                    }))
                                  }
                                }

                                await reloadContext() // 更新があったことをサイドメニューに知らせる
                              }, [reloadContext, queryToTable{{refRepositories.Select(x => $", commit{x.RefFrom.MainAggregate.Item.PhysicalName}").Join("")}}])
                            """)}}

                              return {
                                ready: ready2{{refRepositories.Select(x => $" && {x.RefFrom.MainAggregate.Item.PhysicalName}IsReady").Join("")}},
                                load,
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
                var displayData = new DataClassForDisplay(agg);
                var localRepos = new LocalRepository(agg);
                var controller = new Parts.WebClient.Controller(agg.Item);

                return $$"""
                    async (localReposItem: LocalRepositoryStoredItem<AggregateType.{{displayData.TsTypeName}}>) => {
                      const [{ item: saveItem }] = AggregateType.{{displayData.ConvertFnNameToLocalRepositoryType}}(localReposItem.item)
                      const state = getLocalRepositoryState(localReposItem)
                      if (state === '+') {
                        const url = `{{controller.CreateCommandApi}}`
                        const response = await post<AggregateType.{{displayData.TsTypeName}}>(url, saveItem)
                        return { commit: response.ok }

                      } else if (state === '*') {
                        const url = `{{controller.UpdateCommandApi}}`
                        const response = await post<AggregateType.{{displayData.TsTypeName}}>(url, saveItem)
                        return { commit: response.ok }
                    
                      } else if (state === '-') {
                        const url = `{{controller.DeleteCommandApi}}`
                        const response = await httpDelete(url, saveItem)
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
                    import { useMsgContext, useToastContext } from './Notification'
                    import { useHttpRequest } from './Http'
                    import { ItemKey, LocalRepositoryContextValue, LocalRepositoryStoredItem, SaveLocalItemHandler, getLocalRepositoryState } from './LocalRepository'
                    import * as AggregateType from '../autogenerated-types'

                    export const useLocalRepositoryCommitHandling = () => {
                      const [, dispatchMsg] = useMsgContext()
                      const [, dispatchToast] = useToastContext()
                      const { post, httpDelete } = useHttpRequest()

                      const saveHandlerMap = useMemo(() => new Map<string, SaveLocalItemHandler<any>>([
                    {{aggregates.SelectTextTemplate(agg => $$"""
                        ['{{new LocalRepository(agg).DataTypeKey}}', {{WithIndent(RenderCommitFunction(agg), "    ")}}],
                    """)}}
                      ]), [post, httpDelete, dispatchMsg, dispatchToast])

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

                        dispatchToast(msg => success
                          ? msg.info('保存しました。')
                          : msg.info('一部のデータの保存に失敗しました。'))

                      }, [saveHandlerMap, dispatchMsg, dispatchToast])
                    }
                    """,
            };
        }
    }
}
