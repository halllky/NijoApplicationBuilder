using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebClient.DataTable;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 一覧画面
    /// </summary>
    internal class MultiView : IReactPage {
        internal MultiView(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        public string Url => $"/{_aggregate.Item.UniqueId}"; // React Router は全角文字非対応なので
        public string DirNameInPageDir => _aggregate.Item.DisplayName.ToFileNameSafe();
        public string ComponentPhysicalName => $"{_aggregate.Item.PhysicalName}MultiView";
        public bool ShowMenu => true;
        public string? LabelInMenu => _aggregate.Item.DisplayName;

        // 画面初期表示時の検索条件をMultiViewに来る前の画面で指定するためのURLクエリパラメータの名前
        private const string URL_KEYWORD = "k";
        private const string URL_FILTER = "f";
        private const string URL_SORT = "s";

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = "list.tsx",
            RenderContent = context => {
                var searchCondition = new SearchCondition(_aggregate);
                var searchResult = new DataClassForDisplay(_aggregate);
                var loadMethod = new LoadMethod(_aggregate);
                var createView = new SingleView(_aggregate, SingleView.E_Type.New);
                var detailView = new SingleView(_aggregate, SingleView.E_Type.ReadOnly);

                const string TO_DETAIL_VIEW = "navigateToSingleView";

                var pageRenderingContext = new FormUIRenderingContext {
                    CodeRenderingContext = context,
                    Register = "registerExCondition",
                    GetReactHookFormFieldPath = vm => vm.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript),
                    RenderReadOnlyStatement = vm => string.Empty, // 検索条件欄の項目が読み取り専用になることはない
                    RenderErrorMessage = vm => throw new InvalidOperationException("検索条件欄では項目ごとにエラーメッセージを表示するという概念が無い"),
                };

                var tableBuilder = new DataTableBuilder(_aggregate, $"AggregateType.{searchResult.TsTypeName}", false)
                    // 行ヘッダ（詳細リンク）
                    .Add(new AdhocColumn {
                        Header = string.Empty,
                        DefaultWidth = 64,
                        EnableResizing = false,
                        CellContents = ctx => $$"""
                        cellProps => {
                          const row = cellProps.row.original
                          const state = Util.getUpdateType(row)

                          return (
                            <div className="flex items-center gap-1 pl-1">
                              <button type="button" onClick={() => {{TO_DETAIL_VIEW}}(row, 'readonly')} className="text-link">詳細</button>
                              <span className="inline-block w-4 text-center">{state}</span>
                            </div>
                          )
                        }
                        """,
                    })
                    // メンバーの列
                    .AddMembers(searchResult);

                return $$"""
                    import React, { useCallback, useEffect, useMemo, useRef, useState, useReducer } from 'react'
                    import { useEvent } from 'react-use-event-hook'
                    import { Link, useLocation } from 'react-router-dom'
                    import { useFieldArray, FormProvider } from 'react-hook-form'
                    import * as Icon from '@heroicons/react/24/outline'
                    import { ImperativePanelHandle, Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
                    import * as Util from '../../util'
                    import * as Input from '../../input'
                    import * as Layout from '../../collection'
                    import * as AggregateType from '../../autogenerated-types'
                    import * as AggregateHook from '../../autogenerated-hooks'
                    import { {{AutoGeneratedCustomizer.USE_CONTEXT}} } from '../../autogenerated-customizer'

                    const VForm2 = Layout.VForm2

                    export default function () {
                      const [, dispatchMsg] = Util.useMsgContext()
                      const [, dispatchToast] = Util.useToastContext()
                      const { get } = Util.useHttpRequest()

                      // 検索条件
                      const [currentPage, dispatchPaging] = useReducer(Util.pagingReducer, { pageIndex: 0 })
                      const rhfSearchMethods = Util.useFormEx<AggregateType.{{searchCondition.TsTypeName}}>({})
                      const {
                        getValues: getConditionValues,
                        registerEx: registerExCondition,
                        reset: resetSearchCondition,
                      } = rhfSearchMethods

                      // 検索結果
                      const { {{LoadMethod.LOAD}}, {{LoadMethod.CURRENT_PAGE_ITEMS}} } = AggregateHook.{{loadMethod.ReactHookName}}()

                      // 検索条件欄の開閉
                      const searchConditionPanelRef = useRef<ImperativePanelHandle>(null)
                      const [collapsed, setCollapsed] = useState(false)
                      const toggleSearchCondition = useCallback(() => {
                        if (searchConditionPanelRef.current?.getCollapsed()) {
                          searchConditionPanelRef.current.expand()
                        } else {
                          searchConditionPanelRef.current?.collapse()
                        }
                      }, [searchConditionPanelRef])

                      // 初期表示時処理
                      const { search: locationSerach } = useLocation()
                      useEffect(() => {
                        const condition = parseQueryParameter(locationSerach)
                        {{LoadMethod.LOAD}}(condition) // 再検索
                        resetSearchCondition(condition) // 画面上の検索条件欄の表示を更新する
                      }, [{{LoadMethod.LOAD}}, locationSerach])

                      // 再読み込み時処理
                      const handleReload = useCallback(() => {
                        const condition = getConditionValues()
                        {{LoadMethod.LOAD}}(condition) // 再検索
                        resetSearchCondition(condition) // 画面上の検索条件欄の表示を更新する
                        searchConditionPanelRef.current?.collapse() // 検索条件欄を閉じる
                      }, [{{LoadMethod.LOAD}}, getConditionValues, resetSearchCondition, searchConditionPanelRef])

                      // クリア時処理
                      const clearSearchCondition = useEvent(() => {
                        resetSearchCondition(AggregateType.{{searchCondition.CreateNewObjectFnName}}())
                        searchConditionPanelRef.current?.expand()
                      })

                      // 画面遷移
                      const {{TO_DETAIL_VIEW}} = Util.{{detailView.NavigateFnName}}()
                      const navigateToCreateView = Util.{{createView.NavigateFnName}}()
                      const onClickCreateViewLink = useEvent(() => navigateToCreateView())

                      // カスタマイズ
                      const { {{HeaderCustsomizeComponent}}, {{AutoGeneratedCustomizer.CUSTOM_UI_COMPONENT}} } = {{AutoGeneratedCustomizer.USE_CONTEXT}}()
                      const tableRef = useRef<Layout.DataTableRef<AggregateType.{{searchResult.TsTypeName}}>>(null)
                      const getSelectedItems = useEvent(() => {
                        return tableRef.current?.getSelectedRows().map(x => x.row) ?? []
                      })

                      // 列定義
                      const columnDefs: Layout.ColumnDefEx<AggregateType.{{searchResult.TsTypeName}}>[] = useMemo(() => [
                        {{WithIndent(tableBuilder.RenderColumnDef(context), "    ")}}
                      ], [get, {{TO_DETAIL_VIEW}}])

                      return (
                        <Layout.PageFrame
                          header={<>
                            <Layout.PageTitle className="self-center">
                              {{_aggregate.Item.DisplayName}}
                            </Layout.PageTitle>
                            <div className="flex-1"></div>
                    {{If(!_aggregate.Item.Options.IsReadOnlyAggregate, () => $$"""
                            <Input.IconButton className="self-center" onClick={onClickCreateViewLink}>新規作成</Input.IconButton>
                    """)}}
                            {{{HeaderCustsomizeComponent}} && (
                              <{{HeaderCustsomizeComponent}} getSelectedItems={getSelectedItems} />
                            )}
                            <Input.IconButton className="self-center" onClick={clearSearchCondition}>クリア</Input.IconButton>
                            <div className="self-center flex">
                              <Input.IconButton icon={Icon.MagnifyingGlassIcon} fill onClick={handleReload}>検索</Input.IconButton>
                              <div className="self-stretch w-px bg-color-base"></div>
                              <Input.IconButton icon={collapsed ? Icon.ChevronDownIcon : Icon.ChevronUpIcon} fill onClick={toggleSearchCondition} hideText>検索条件</Input.IconButton>
                            </div>
                          </>}
                        >

                          <PanelGroup direction="vertical">

                            {/* 検索条件欄 */}
                            <Panel ref={searchConditionPanelRef} defaultSize={30} collapsible onCollapse={setCollapsed}>
                              <div className="h-full overflow-y-scroll border border-color-4 bg-color-gutter">
                                <FormProvider {...rhfSearchMethods}>
                                  {{WithIndent(searchCondition.RenderVForm2(pageRenderingContext), "              ")}}
                                </FormProvider>
                              </div>
                            </Panel>

                            <PanelResizeHandle className="h-2" />

                            {/* 検索結果欄 */}
                            <Panel>
                              <Layout.DataTable
                                ref={tableRef}
                                data={{{LoadMethod.CURRENT_PAGE_ITEMS}}}
                                columns={columnDefs}
                                className="h-full border border-color-4"
                              ></Layout.DataTable>
                            </Panel>
                          </PanelGroup>

                        </Layout.PageFrame>
                      )
                    }

                    /** クエリパラメータを解釈して画面初期表示時検索条件オブジェクトを返します。 */
                    function parseQueryParameter(url: string): AggregateType.{{searchCondition.TsTypeName}} {
                      const searchCondition = AggregateType.{{searchCondition.CreateNewObjectFnName}}()
                      if (!url) return searchCondition

                      const searchParams = new URLSearchParams(new URL(url).search)
                      if (searchParams.has('{{URL_KEYWORD}}'))
                        searchCondition.{{SearchCondition.KEYWORD_TS}} = searchParams.get('{{URL_KEYWORD}}')!
                      if (searchParams.has('{{URL_FILTER}}'))
                        searchCondition.{{SearchCondition.FILTER_TS}} = JSON.parse(searchParams.get('{{URL_FILTER}}')!)
                      if (searchParams.has('{{URL_SORT}}'))
                        searchCondition.{{SearchCondition.SORT_TS}} = JSON.parse(searchParams.get('{{URL_SORT}}')!)

                      return searchCondition
                    }
                    """;
            },
        };

        internal string NavigationHookName => $"useNavigateTo{_aggregate.Item.PhysicalName}MultiView";

        internal string RenderNavigationHook(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);
            return $$"""
                /** {{_aggregate.Item.DisplayName}}の一覧検索画面へ遷移します。初期表示時検索条件を指定することができます。 */
                export const {{NavigationHookName}} = () => {
                  const navigate = ReactRouter.useNavigate()

                  /** {{_aggregate.Item.DisplayName}}の一覧検索画面へ遷移します。初期表示時検索条件を指定することができます。 */
                  return React.useCallback((init?: Types.{{searchCondition.TsTypeName}}) => {
                    // 初期表示時検索条件の設定
                    const searchParams = new URLSearchParams()
                    if (init !== undefined) {
                      searchParams.append('{{URL_FILTER}}', JSON.stringify(init.{{SearchCondition.FILTER_TS}}))
                      if (init.{{SearchCondition.KEYWORD_TS}}) searchParams.append('{{URL_KEYWORD}}', init.{{SearchCondition.KEYWORD_TS}})
                      if (init.{{SearchCondition.SORT_TS}}.length > 0) searchParams.append('{{URL_SORT}}', JSON.stringify(init.{{SearchCondition.SORT_TS}}))
                    }

                    navigate({
                      pathname: '{{Url}}',
                      search: searchParams.toString()
                    })
                  }, [navigate])
                }
                """;
        }

        #region ヘッダ部のカスタマイズ部分
        private string HeaderCustsomizeComponent => $"Into{ComponentPhysicalName}Header";
        internal string RenderHeaderCustomizeType() {
            var searchResult = new DataClassForDisplay(_aggregate);
            return $$"""
                {{HeaderCustsomizeComponent}}?: (props: {
                  getSelectedItems: (() => AggregateType.{{searchResult.TsTypeName}}[])
                }) => React.ReactNode
                """;
        }
        #endregion ヘッダ部のカスタマイズ部分
    }
}