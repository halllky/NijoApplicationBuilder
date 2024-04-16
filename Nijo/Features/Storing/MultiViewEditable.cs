using Nijo.Core;
using Nijo.Parts.Utility;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    /// <summary>
    /// 編集可能なマルチビュー。LocalRepositoryの仕組みに大きく依存している。
    /// 
    /// TODO: ソート
    /// </summary>
    internal class MultiViewEditable : IReactPage {

        internal class Options {
            internal bool ReadOnly { get; init; } = false;
            internal string? Hooks { get; init; } = null;
            internal string? PageTitleSide { get; init; } = null;
        }

        internal MultiViewEditable(GraphNode<Aggregate> aggregate, Options? options = null) {
            if (!aggregate.IsRoot()) throw new ArgumentException("Editable multi view requires root aggregate.");
            _aggregate = aggregate;
            _options = options ?? new();
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly Options _options;

        public string Url => $"/{_aggregate.Item.DisplayName.ToHashedString()}";
        string IReactPage.DirNameInPageDir => _aggregate.Item.DisplayName.ToFileNameSafe();
        string IReactPage.ComponentPhysicalName => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}MultiView";
        bool IReactPage.ShowMenu => true;
        string? IReactPage.LabelInMenu => _aggregate.Item.DisplayName;

        SourceFile IReactPage.GetSourceFile() {
            var singleView = new SingleView(_aggregate, SingleView.E_Type.View);
            var editView = new SingleView(_aggregate, SingleView.E_Type.Edit);
            var createView = new SingleView(_aggregate, SingleView.E_Type.Create);
            var findMany = new FindManyFeature(_aggregate);
            var createEmptyObject = new TSInitializerFunction(_aggregate).FunctionName;

            var keys = _aggregate
               .GetKeys()
               .OfType<AggregateMember.ValueMember>()
               .ToArray();
            var names = _aggregate
                .GetNames()
                .OfType<AggregateMember.ValueMember>()
                .ToArray();

            var groupedSearchConditions = findMany
                .EnumerateSearchConditionMembers()
                .Where(vm => !vm.Options.InvisibleInGui)
                .GroupBy(vm => vm.DeclaringAggregate)
                // 自身のメンバーを検索条件の先頭に表示する
                .OrderBy(group => group.Key == _aggregate ? 1 : 2);

            var rowHeader = new DataTableColumn {
                Id = "col0",
                Header = string.Empty,
                Size = 64,
                EnableResizing = false,
                Cell = $$"""
                    cellProps => {
                      const row = cellProps.row.original.item
                    {{If(_options.ReadOnly, () => $$"""
                      const singleViewUrl = `{{singleView.GetUrlStringForReact(keys.Select(k => $"row.item.{k.Declared.GetFullPath().Join("?.")}"))}}`
                    """).Else(() => $$"""
                      const singleViewUrl = row.state === '+'
                        ? `{{createView.GetUrlStringForReact(new[] { "row.itemKey" })}}`
                        : `{{editView.GetUrlStringForReact(keys.Select(k => $"row.item.{k.Declared.GetFullPath().Join("?.")}"))}}`
                    """)}}
                      return (
                        <div className="flex items-center gap-1 pl-1">
                          <Link to={singleViewUrl} className="text-link">詳細</Link>
                          <span className="inline-block w-4 text-center">{row.state}</span>
                        </div>
                      )
                    }
                    """,
            };
            var gridColumns = new[] { rowHeader }.Concat(_aggregate
                .EnumerateThisAndDescendants()
                // ChildrenやVariationのメンバーはグリッド上で表現できないため表示しない
                .Where(agg => agg.EnumerateAncestorsAndThis().All(agg2 => agg2.IsRoot() || agg2.IsChildMember()))
                .SelectMany(agg => agg.GetMembers())
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => !vm.Options.InvisibleInGui)
                .Select((vm, ix) => DataTableColumn.FromMember(vm, "item.item", _aggregate, $"col{ix + 1}", _options.ReadOnly)));

            return new SourceFile {
                FileName = "list.tsx",
                RenderContent = context => $$"""
                    import React, { useCallback, useEffect, useMemo, useRef, useState, useReducer } from 'react'
                    import { Link } from 'react-router-dom'
                    import { useFieldArray, FormProvider } from 'react-hook-form'
                    import dayjs from 'dayjs'
                    import * as Util from '../../util'
                    import * as Input from '../../input'
                    import * as Layout from '../../collection'
                    import * as AggregateType from '../../autogenerated-types'

                    const VForm = Layout.VerticalForm

                    export default function () {
                      const [, dispatchMsg] = Util.useMsgContext()

                      // リモートリポジトリ（APサーバー）
                      const [remoteItems, setRemoteItems] = useState<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}[]>(() => [])
                      const { post } = Util.useHttpRequest()

                      // ローカルリポジトリ（IndexedDB）
                      const reposSetting: Util.LocalRepositoryArgs<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}> = useMemo(() => ({
                        dataTypeKey: '{{LocalRepository.GetDataTypeKey(_aggregate)}}',
                        getItemKey: x => JSON.stringify([{{keys.Select(k => $"x.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]),
                        getItemName: x => `{{string.Concat(names.Select(n => $"${{x.{n.Declared.GetFullPath().Join("?.")}}}"))}}`,
                        remoteItems: remoteItems,
                      }), [remoteItems])
                      const {
                        ready,
                        loadLocalItems,
                        addToLocalRepository,
                        updateLocalRepositoryItem,
                        deleteLocalRepositoryItem,
                      } = Util.useLocalRepository(reposSetting)

                      // 編集対象（リモートリポジトリ + ローカルリポジトリ）
                      const reactHookFormMethods = Util.useFormEx<{ currentPageItems: GridRow[] }>({})
                      const { control, registerEx, handleSubmit, reset } = reactHookFormMethods
                      const { fields, append, update, remove } = useFieldArray({ name: 'currentPageItems', control })

                      // 検索条件
                      const rhfSearchMethods = Util.useFormEx<AggregateType.{{findMany.TypeScriptConditionClass}}>({})
                      const getConditionValues = rhfSearchMethods.getValues
                      const registerExCondition = rhfSearchMethods.registerEx

                      // ページングとデータ読み込み
                      const [currentPage, dispatchPaging] = useReducer(pagingReducer, { pageIndex: 0 })
                      const reloadRemoteItems = useCallback(async () => {
                        if (!ready) return

                        const skip = currentPage.pageIndex * 20
                        const take = 20
                        const url = `{{findMany.GetUrlStringForReact()}}?{{FindManyFeature.PARAM_SKIP}}=${skip}&{{FindManyFeature.PARAM_TAKE}}=${take}`
                        const filters = getConditionValues()
                        const response = await post<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}[]>(url, filters)

                        setRemoteItems(response.ok ? response.data : [])

                        dispatchPaging(page => page.loadComplete())
                      }, [ready, loadLocalItems, getConditionValues, currentPage, reset, dispatchPaging])

                      // TODO: ↓ここのデータの流れがややこしい
                      useEffect(() => {
                        if (!currentPage.loaded) reloadRemoteItems()
                      }, [currentPage, reloadRemoteItems])

                      useEffect(() => {
                        if (!ready) return
                        if (!currentPage.loaded) return
                        loadLocalItems().then(currentPageItems => reset({ currentPageItems }))
                      }, [ready, currentPage, loadLocalItems, reset])

                      const dtRef = useRef<Layout.DataTableRef<GridRow>>(null)
                    {{If(!_options.ReadOnly, () => $$"""

                      // データ編集
                      const handleAdd: React.MouseEventHandler<HTMLButtonElement> = useCallback(async () => {
                        const newItem = AggregateType.{{createEmptyObject}}()
                        append(await addToLocalRepository(newItem))
                      }, [append, addToLocalRepository])

                      const handleUpdateRow = useCallback(async (index: number, row: GridRow) => {
                        update(index, await updateLocalRepositoryItem(row.itemKey, row.item))
                      }, [update, updateLocalRepositoryItem])

                      const handleRemove: React.MouseEventHandler<HTMLButtonElement> = useCallback(async () => {
                        if (!dtRef.current) return
                        const deletedRowIndex: number[] = []
                        for (const { row, rowIndex } of dtRef.current.getSelectedRows()) {
                          const deleted = await deleteLocalRepositoryItem(row.itemKey, row.item)
                          if (deleted) update(rowIndex, deleted)
                          else deletedRowIndex.push(rowIndex)
                        }
                        remove(deletedRowIndex)
                      }, [update, remove, deleteLocalRepositoryItem])
                    """)}}

                    {{If(_options.Hooks != null, () => $$"""
                      {{WithIndent(_options.Hooks!, "  ")}}

                    """)}}
                      return (
                        <div className="page-content-root gap-4 pb-[50vh]">

                          <FormProvider {...rhfSearchMethods}>
                            <form className="flex flex-col">
                              <div className="flex gap-2 justify-start">
                                <h1 className="text-base font-semibold select-none py-1">
                                  {{_aggregate.Item.DisplayName}}
                                </h1>
                                <Input.Button onClick={reloadRemoteItems}>再読み込み</Input.Button>
                                <div className="basis-4"></div>
                    {{If(!_options.ReadOnly, () => $$"""
                                <Input.Button onClick={handleAdd}>追加</Input.Button>
                                <Input.Button onClick={handleRemove}>削除</Input.Button>
                    """)}}
                    {{If(_options.PageTitleSide != null, () => $$"""
                                {{WithIndent(_options.PageTitleSide!, "            ")}}
                    """)}}
                              </div>
                              <VForm.Container leftColumnMinWidth="10rem">
                    {{groupedSearchConditions.SelectTextTemplate(group => $$"""
                    {{If(group.Key == _aggregate, () => $$"""
                                {{WithIndent(group.SelectTextTemplate(RenderSearchConditionValueMember), "            ")}}
                    """).Else(() => $$"""
                                <VForm.Container label="{{group.Key.Item.DisplayName}}">
                                  {{WithIndent(group.SelectTextTemplate(RenderSearchConditionValueMember), "              ")}}
                                </VForm.Container>
                    """)}}
                    """)}}
                              </VForm.Container>
                            </form>
                          </FormProvider>

                          <FormProvider {...reactHookFormMethods}>
                            <form className="flex-1">
                              <Layout.DataTable
                                ref={dtRef}
                                data={fields}
                                columns={COLUMN_DEFS}
                    {{If(!_options.ReadOnly, () => $$"""
                                onChangeRow={handleUpdateRow}
                    """)}}
                                className="h-full"
                              ></Layout.DataTable>
                            </form>
                          </FormProvider>
                        </div>
                      )
                    }

                    type GridRow = Util.LocalRepositoryItem<AggregateType.{{_aggregate.Item.TypeScriptTypeName}}>

                    const COLUMN_DEFS: Layout.ColumnDefEx<Util.TreeNode<GridRow>>[] = [
                      {{WithIndent(gridColumns.SelectTextTemplate(col => col.Render()), "  ")}}
                    ]

                    // TODO: utilに持っていく
                    type PageState = { pageIndex: number, loaded?: boolean }
                    const pagingReducer = Util.defineReducer((state: PageState) => ({
                      loadComplete: () => ({ pageIndex: state.pageIndex, loaded: true }),
                      nextPage: () => ({ pageIndex: state.pageIndex + 1, loaded: false }),
                      prevPage: () => ({ pageIndex: Math.max(0, state.pageIndex - 1), loaded: false }),
                      moveTo: (pageIndex: number) => ({ pageIndex, loaded: false }),
                    }))
                    """,
            };
        }

        private static string RenderSearchConditionValueMember(AggregateMember.ValueMember vm) {
            var component = vm.Options.MemberType.GetReactComponent(new() {
                Type = GetReactComponentArgs.E_Type.InDetailView,
            });

            return $$"""
                <VForm.Item label="{{vm.MemberName}}">
                {{If(vm is AggregateMember.Variation, () => FindManyFeature.VariationMemberProps((AggregateMember.Variation)vm).SelectTextTemplate(x => $$"""
                  <label className="inline-flex items-center">
                    <Input.CheckBox {...registerExCondition(`{{vm.Declared.GetFullPath().SkipLast(1).Concat(new[] { x.Value }).Join(".")}}`)} />
                    {{x.Key.MemberName}}
                  </label>
                """)).ElseIf(vm.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                  <{{component.Name}} {...registerExCondition(`{{vm.Declared.GetFullPath().Join(".")}}.{{FromTo.FROM}}`)}{{string.Concat(component.GetPropsStatement())}} />
                  <span className="select-none">～</span>
                  <{{component.Name}} {...registerExCondition(`{{vm.Declared.GetFullPath().Join(".")}}.{{FromTo.TO}}`)}{{string.Concat(component.GetPropsStatement())}} />
                """).Else(() => $$"""
                  <{{component.Name}} {...registerExCondition(`{{vm.Declared.GetFullPath().Join(".")}}`)}{{string.Concat(component.GetPropsStatement())}} />
                """)}}
                </VForm.Item>
                """;
        }
    }
}
