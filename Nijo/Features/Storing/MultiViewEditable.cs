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
            return new SourceFile {
                FileName = "list.tsx",
                RenderContent = context => {
                    var dataClass = new DataClassForDisplay(_aggregate);
                    var findMany = new FindManyFeature(_aggregate);
                    var rootLocalRepository = new LocalRepository(_aggregate);
                    var navigation = new NavigationWrapper(_aggregate);

                    var groupedSearchConditions = findMany
                        .EnumerateSearchConditionMembers()
                        .Where(vm => !vm.Options.InvisibleInGui)
                        .GroupBy(vm => vm.DeclaringAggregate)
                        // 自身のメンバーを検索条件の先頭に表示する
                        .OrderBy(group => group.Key == _aggregate ? 1 : 2);

                    // 一覧画面ではその性質上ChildrenやVariationの項目を編集できないため、
                    // 一時保存が無効化されるとSingleViewへの画面遷移が必須となるので事実上編集不可能になる。
                    var isReadOnly = _options.ReadOnly || context.Config.DisableLocalRepository;

                    // 新規作成画面へのリンクがあるかどうか
                    var linkToCreateView = !_options.ReadOnly
                        && context.Config.DisableLocalRepository
                        && _aggregate.GetSingleRefKeyAggregate() == null;

                    var rowHeader = new DataTableColumn {
                        DataTableRowTypeName = "GridRow",
                        Id = "col-header",
                        Header = string.Empty,
                        Size = 64,
                        EnableResizing = false,
                        Cell = $$"""
                    cellProps => {
                      const row = cellProps.row.original
                      const state = Util.getLocalRepositoryState(row)
                    {{If(isReadOnly, () => $$"""
                      const singleViewUrl = Util.{{navigation.GetSingleViewUrlHookName}}(row.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}}, 'view')
                    """).Else(() => $$"""
                      const singleViewUrl = Util.{{navigation.GetSingleViewUrlHookName}}(row.{{DataClassForDisplay.LOCAL_REPOS_ITEMKEY}}, state === '+' ? 'new' : 'edit')
                    """)}}
                      return (
                        <div className="flex items-center gap-1 pl-1">
                          <Link to={singleViewUrl} className="text-link">詳細</Link>
                          <span className="inline-block w-4 text-center">{state}</span>
                        </div>
                      )
                    }
                    """,
                    };
                    var gridColumns = new[] { rowHeader }.Concat(DataTableColumn.FromMembers(
                        "GridRow",
                        _aggregate,
                        isReadOnly,
                        useFormContextType: "{ currentPageItems: GridRow[] }",
                        registerPathModifier: path => $"currentPageItems.${{row.index}}.{path}"));

                    return $$"""
                        import React, { useCallback, useEffect, useMemo, useRef, useState, useReducer } from 'react'
                        import { Link } from 'react-router-dom'
                        import { useFieldArray, FormProvider } from 'react-hook-form'
                        import { BookmarkSquareIcon, PencilIcon, XMarkIcon, PlusIcon, ChevronDownIcon, ChevronUpIcon, MagnifyingGlassIcon } from '@heroicons/react/24/outline'
                        import { ImperativePanelHandle, Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
                        import dayjs from 'dayjs'
                        import { UUID } from 'uuidjs'
                        import * as Util from '../../util'
                        import * as Input from '../../input'
                        import * as Layout from '../../collection'
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
                          const [, dispatchMsg] = Util.useMsgContext()
                          const [, dispatchToast] = Util.useToastContext()
                          const { get } = Util.useHttpRequest()

                          // 検索条件
                          const [filter, setFilter] = useState<AggregateType.{{findMany.TypeScriptConditionClass}}>(() => AggregateType.{{findMany.TypeScriptConditionInitializerFn}}())
                          const [currentPage, dispatchPaging] = useReducer(pagingReducer, { pageIndex: 0 })
                          const searchConditionPanelRef = useRef<ImperativePanelHandle>(null)
                          const [collapsed, setCollapsed] = useState(false)

                          const rhfSearchMethods = Util.useFormEx<AggregateType.{{findMany.TypeScriptConditionClass}}>({})
                          const {
                            getValues: getConditionValues,
                            registerEx: registerExCondition,
                            reset: resetSearchCondition,
                          } = rhfSearchMethods
                          const clearSearchCondition = useCallback(() => {
                            resetSearchCondition()
                            searchConditionPanelRef.current?.expand()
                          }, [resetSearchCondition, searchConditionPanelRef])
                          const toggleSearchCondition = useCallback(() => {
                            if (searchConditionPanelRef.current?.getCollapsed()) {
                              searchConditionPanelRef.current.expand()
                            } else {
                              searchConditionPanelRef.current?.collapse()
                            }
                          }, [searchConditionPanelRef])

                          // 編集対象（リモートリポジトリ + ローカルリポジトリ）
                          const editRange = useMemo(() => ({
                            filter,
                            skip: currentPage.pageIndex * 20,
                            take: 20,
                          }), [filter, currentPage])
                          const { load{{(isReadOnly ? "" : ", commit")}} } = Util.{{rootLocalRepository.HookName}}(editRange)

                          const reactHookFormMethods = Util.useFormEx<{ currentPageItems: GridRow[] }>({})
                          const { control, registerEx, handleSubmit, reset } = reactHookFormMethods
                          const { fields, append, update, remove } = useFieldArray({ name: 'currentPageItems', control })

                          // 画面表示時、再読み込み時
                          useEffect(() => {
                            load().then(currentPageItems => {
                              if (currentPageItems) {
                                reset({ currentPageItems })
                              }
                            })
                          }, [load])

                          const handleReload = useCallback(() => {
                            setFilter(getConditionValues())
                            searchConditionPanelRef.current?.collapse()
                          }, [getConditionValues, searchConditionPanelRef])

                        {{If(!isReadOnly && _aggregate.GetSingleRefKeyAggregate() == null, () => $$"""
                          // データ編集
                          const handleAdd: React.MouseEventHandler<HTMLButtonElement> = useCallback(async () => {
                            const newRow: AggregateType.{{dataClass.TsTypeName}} = {{WithIndent(dataClass.RenderNewObjectLiteral(), "    ")}}
                            append(newRow)
                          }, [append])

                        """)}}
                        {{If(!isReadOnly, () => $$"""
                          // データ編集
                          const handleUpdateRow = useCallback(async (index: number, row: GridRow) => {
                            update(index, { ...row, {{DataClassForDisplay.WILL_BE_CHANGED}}: true })
                          }, [update])

                          const dtRef = useRef<Layout.DataTableRef<GridRow>>(null)
                          const handleRemove: React.MouseEventHandler<HTMLButtonElement> = useCallback(async () => {
                            if (!dtRef.current) return
                            for (const { row, rowIndex } of dtRef.current.getSelectedRows()) {
                              update(rowIndex, { ...row, {{DataClassForDisplay.WILL_BE_DELETED}}: true })
                            }
                          }, [update])

                          // データの一時保存
                          const onSave = useCallback(async () => {
                            await commit(...fields)
                            const currentPageItems = await load()
                            if (currentPageItems) reset({ currentPageItems })
                          }, [commit, load, fields])

                        """)}}
                        {{If(_options.Hooks != null, () => $$"""
                          {{WithIndent(_options.Hooks!, "  ")}}

                        """)}}
                          // 列定義
                          const columnDefs: Layout.ColumnDefEx<GridRow>[] = useMemo(() => [
                            {{WithIndent(gridColumns.SelectTextTemplate(col => col.Render()), "    ")}}
                          ], [get, update])

                          return (
                            <div className="page-content-root">

                              <div className="flex gap-4 p-1">
                                <div className="flex gap-4 flex-wrap">
                                  <Util.SideMenuCollapseButton />
                                  <h1 className="self-center text-base font-semibold whitespace-nowrap select-none">
                                    {{_aggregate.Item.DisplayName}}
                                  </h1>
                        {{If(linkToCreateView, () => $$"""
                                  <Link to={Util.{{navigation.GetSingleViewUrlHookName}}(undefined, 'new')} className="self-center">新規作成</Link>
                        """)}}
                        {{If(!isReadOnly && _aggregate.GetSingleRefKeyAggregate() == null, () => $$"""
                                  <Input.IconButton className="self-center" onClick={handleAdd}>追加</Input.IconButton>
                        """)}}
                        {{If(!isReadOnly, () => $$"""
                                  <Input.IconButton className="self-center" onClick={handleRemove}>削除</Input.IconButton>
                                  <Input.IconButton className="self-center" onClick={onSave}>一時保存</Input.IconButton>
                        """)}}
                        {{If(_options.PageTitleSide != null, () => $$"""
                                {{WithIndent(_options.PageTitleSide!, "        ")}}
                        """)}}
                                </div>
                                <div className="flex-1"></div>
                                <Input.IconButton className="self-center" onClick={clearSearchCondition}>クリア</Input.IconButton>
                                <div className="self-center flex">
                                  <Input.IconButton icon={MagnifyingGlassIcon} fill onClick={handleReload}>検索</Input.IconButton>
                                  <div className="self-stretch w-px bg-color-base"></div>
                                  <Input.IconButton icon={collapsed ? ChevronDownIcon : ChevronUpIcon} fill onClick={toggleSearchCondition} hideText>検索条件</Input.IconButton>
                                </div>
                              </div>

                              <PanelGroup direction="vertical">
                                <Panel ref={searchConditionPanelRef} defaultSize={30} collapsible onCollapse={setCollapsed}>
                                  <div className="h-full overflow-auto">
                                    <FormProvider {...rhfSearchMethods}>
                                      <VForm.Container estimatedLabelWidth="10rem" className="p-1">
                        {{groupedSearchConditions.SelectTextTemplate(group => $$"""
                        {{If(group.Key == _aggregate, () => $$"""
                                        {{WithIndent(group.SelectTextTemplate(RenderSearchConditionValueMember), "                ")}}
                        """).Else(() => $$"""
                                        <VForm.Container label="{{group.Key.Item.DisplayName}}">
                                          {{WithIndent(group.SelectTextTemplate(RenderSearchConditionValueMember), "                  ")}}
                                        </VForm.Container>
                        """)}}
                        """)}}
                                      </VForm.Container>
                                    </FormProvider>
                                  </div>
                                </Panel>

                                <PanelResizeHandle className="h-2 bg-color-4" />

                                <Panel>
                                  <Util.InlineMessageList />
                                  <FormProvider {...reactHookFormMethods}>
                                    <Layout.DataTable
                                      data={fields}
                                      columns={columnDefs}
                        {{If(!isReadOnly, () => $$"""
                                      onChangeRow={handleUpdateRow}
                                      ref={dtRef}
                        """)}}
                                      className="h-full"
                                    ></Layout.DataTable>
                                  </FormProvider>
                                </Panel>
                              </PanelGroup>
                            </div>
                          )
                        }

                        type GridRow = AggregateType.{{dataClass.TsTypeName}}

                        // TODO: utilに持っていく
                        type PageState = { pageIndex: number, loaded?: boolean }
                        const pagingReducer = Util.defineReducer((state: PageState) => ({
                          loadComplete: () => ({ pageIndex: state.pageIndex, loaded: true }),
                          nextPage: () => ({ pageIndex: state.pageIndex + 1, loaded: false }),
                          prevPage: () => ({ pageIndex: Math.max(0, state.pageIndex - 1), loaded: false }),
                          moveTo: (pageIndex: number) => ({ pageIndex, loaded: false }),
                        }))
                        """;
                },
            };
        }

        private static string RenderSearchConditionValueMember(AggregateMember.ValueMember vm) {
            var component = vm.Options.MemberType.GetReactComponent();

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
