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
                        .Where(agg => agg.Item.Options.Handler == NijoCodeGenerator.Models.WriteModel.Key);

                    var convesionBetweenDisplayDataAndTranScopeDataHooks = aggregates.SelectTextTemplate(agg => {
                        var dataClass = new DisplayDataClass(agg);
                        var localRepositosy = new LocalRepository(agg);
                        var keys = agg.GetKeys().OfType<AggregateMember.ValueMember>();
                        var names = agg.GetNames().OfType<AggregateMember.ValueMember>();
                        var keyArray = KeyArray.Create(agg);
                        var find = new FindFeature(agg);
                        var findMany = new FindManyFeature(agg);
                        var refRepositories = dataClass
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
                            /** {{agg.Item.DisplayName}}の画面に表示するデータ型と登録更新するデータ型の変換を行うフック */
                            export const {{localRepositosy.HookName}} = (editRange?
                              // 複数件編集の場合
                              : { filter: AggregateType.{{findMany.TypeScriptConditionClass}}, skip?: number, take?: number }
                              // 1件編集の場合
                              | [{{keyArray.Select(k => $"{k.VarName}: {k.TsType} | undefined").Join(", ")}}]
                            ) => {

                              // {{agg.Item.DisplayName}}のローカルリポジトリとリモートリポジトリへのデータ読み書き処理
                              const {
                                ready,
                                items: {{agg.Item.ClassName}}Items,
                                commit: commit{{agg.Item.ClassName}},
                              } = {{localRepositosy.LocalLoaderHookName}}(editRange)
                            {{refRepositories.SelectTextTemplate(x => $$"""

                              // {{x.Aggregate.Item.DisplayName}}のローカルリポジトリとリモートリポジトリへのデータ読み書き処理
                              const {{x.Aggregate.Item.ClassName}}filter: { filter: AggregateType.{{x.FindMany.TypeScriptConditionClass}} } = useMemo(() => {
                                const f = AggregateType.{{x.FindMany.TypeScriptConditionInitializerFn}}()
                                if (Array.isArray(editRange)) {
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
                                commit: commit{{x.Aggregate.Item.ClassName}},
                              } = {{x.Repos.LocalLoaderHookName}}({{x.Aggregate.Item.ClassName}}filter)
                            """)}}

                              // 登録更新のデータ型を画面表示用のデータ型に変換する
                              const [items, setItems] = useState<AggregateType.{{dataClass.TsTypeName}}[]>(() => [])
                              const allReady = ready{{refRepositories.Select(r => $" && {r.Aggregate.Item.ClassName}IsReady").Join("")}}
                              useEffect(() => {
                                if (allReady) {
                                  const currentPageItems: AggregateType.{{dataClass.TsTypeName}}[] = {{agg.Item.ClassName}}Items.map(item => {
                                    return AggregateType.{{dataClass.ConvertFnNameToDisplayDataType}}(item{{refRepositories.Select(r => $", {r.Aggregate.Item.ClassName}Items").Join("")}})
                                  })
                                  setItems(currentPageItems)
                                }
                              }, [allReady, {{agg.Item.ClassName}}Items{{refRepositories.Select(r => $", {r.Aggregate.Item.ClassName}Items").Join("")}}])

                              // 保存
                              const commit = useCallback(async (...commitItems: AggregateType.{{dataClass.TsTypeName}}[]) => {

                                // 画面表示用のデータ型を登録更新のデータ型に変換する
                                const arr{{agg.Item.ClassName}}: LocalRepositoryItem<AggregateType.{{agg.Item.TypeScriptTypeName}}>[] = []
                            {{refRepositories.SelectTextTemplate(x => $$"""
                                const arr{{x.Aggregate.Item.ClassName}}: LocalRepositoryItem<AggregateType.{{x.Aggregate.Item.TypeScriptTypeName}}>[] = []
                            """)}}
                                for (const item of commitItems) {
                                  const [
                                    item{{agg.Item.ClassName}}{{dataClass.GetRefFromPropsRecursively().Select((x, i) => $", item{i}_{x.Item1.MainAggregate.Item.ClassName}").Join("")}}
                                  ] = AggregateType.{{dataClass.ConvertFnNameToLocalRepositoryType}}(item)

                                  arr{{agg.Item.ClassName}}.push(item{{agg.Item.ClassName}})
                            {{dataClass.GetRefFromPropsRecursively().SelectTextTemplate((x, i) => x.IsArray ? $$"""
                                  arr{{x.Item1.MainAggregate.Item.ClassName}}.push(...item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}})
                            """ : $$"""
                                  if (item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}}) arr{{x.Item1.MainAggregate.Item.ClassName}}.push(item{{i}}_{{x.Item1.MainAggregate.Item.ClassName}})
                            """)}}
                                }

                                // リポジトリへ反映する
                                await commit{{agg.Item.ClassName}}(...arr{{agg.Item.ClassName}})
                            {{dataClass.GetRefFromPropsRecursively().SelectTextTemplate((x, i) => $$"""
                                await commit{{x.Item1.MainAggregate.Item.ClassName}}(...arr{{x.Item1.MainAggregate.Item.ClassName}})
                            """)}}
                              }, [commit{{agg.Item.ClassName}}{{refRepositories.Select(x => $", commit{x.Aggregate.Item.ClassName}").Join("")}}])

                              return { ready: allReady, items, commit }
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

                        return $$"""
                            /** {{agg.Item.DisplayName}}専用のuseLocalRepositoryのラッパー */
                            const {{localRepositosy.LocalLoaderHookName}} = (editRange?
                              // 複数件編集の場合
                              : { filter: AggregateType.{{findMany.TypeScriptConditionClass}}, skip?: number, take?: number }
                              // 1件編集の場合
                              | [{{keyArray.Select(k => $"{k.VarName}: {k.TsType} | undefined").Join(", ")}}]
                            ) => {
                              const { get, post } = useHttpRequest()
                              const loadRemoteItems = useCallback(async () => {
                                if (editRange === undefined) return [] // 画面表示直後の検索条件が決まっていない場合など

                                let remote: AggregateType.{{agg.Item.TypeScriptTypeName}}[]
                                if (Array.isArray(editRange)) {
                            {{keyArray.SelectTextTemplate((_, i) => $$"""
                                  if (editRange[{{i}}] === undefined) return []
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

                                return remote
                              }, [editRange, get, post])

                              // ローカルリポジトリ設定
                              const localReposSetting: LocalRepositoryArgs<AggregateType.{{agg.Item.TypeScriptTypeName}}> = useMemo(() => ({
                                dataTypeKey: '{{localRepositosy.DataTypeKey}}',
                                getItemKey: x => JSON.stringify([{{keys.Select(k => $"x.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]),
                                getItemName: x => `{{string.Concat(names.Select(n => $"${{x.{n.Declared.GetFullPath().Join("?.")}}}"))}}`,
                                loadRemoteItems,
                              }), [loadRemoteItems])

                              return useLocalRepository(localReposSetting)
                            }
                            """;
                    });

                    return $$"""
                        import { useState, useMemo, useCallback, useEffect } from 'react'
                        import { UUID } from 'uuidjs'
                        import { useHttpRequest } from './Http'
                        import { useLocalRepository, LocalRepositoryArgs, LocalRepositoryItem } from './LocalRepository'
                        import { visitObject } from './Tree'
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
