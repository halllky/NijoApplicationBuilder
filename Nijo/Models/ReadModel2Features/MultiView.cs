using Nijo.Core;
using Nijo.Parts;
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
    internal class MultiView {
        internal MultiView(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        public string Url => $"/{(_aggregate.Item.Options.LatinName ?? _aggregate.Item.UniqueId).ToKebabCase()}"; // React Router は全角文字非対応なので
        public string DirNameInPageDir => _aggregate.Item.PhysicalName.ToFileNameSafe();
        public string ComponentPhysicalName => $"{_aggregate.Item.PhysicalName}MultiView";
        public string UiContextSectionName => ComponentPhysicalName;
        public bool ShowMenu => true;
        public string? LabelInMenu => _aggregate.Item.DisplayName;

        internal const string PAGE_SIZE_COMBO_SETTING = "pageSizeComboSetting";
        internal const string SORT_COMBO_SETTING = "sortComboSetting";
        internal const string SORT_COMBO_FILTERING = "onFilterSortCombo";

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = "multi-view.tsx",
            RenderContent = context => {
                var searchCondition = new SearchCondition(_aggregate);
                var searchResult = new DataClassForDisplay(_aggregate);
                var loadMethod = new LoadMethod(_aggregate);
                var singleView = new SingleView(_aggregate);
                var multiEditView = new MultiViewEditable(_aggregate);

                const string TO_DETAIL_VIEW = "navigateToSingleView";
                const string TO_MULTI_EDIT_VIEW = "navigateToMultiEditView";

                var pageRenderingContext = new FormUIRenderingContext {
                    CodeRenderingContext = context,
                    Register = "registerExCondition",
                    GetReactHookFormFieldPath = vm => vm.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript),
                    RenderReadOnlyStatement = vm => string.Empty, // 検索条件欄の項目が読み取り専用になることはない
                    RenderErrorMessage = vm => throw new InvalidOperationException("検索条件欄では項目ごとにエラーメッセージを表示するという概念が無い"),
                };

                var tableBuilder = DataTableBuilder.ReadOnlyGrid(_aggregate, $"AggregateType.{searchResult.TsTypeName}")
                    // 行ヘッダ（詳細リンク）
                    .Add(new AdhocColumn {
                        Header = string.Empty,
                        DefaultWidth = 64,
                        EnableResizing = false,
                        CellContents = (ctx, arg, argRowObject) => $$"""
                        {{arg}} => (
                          <button type="button" onClick={() => {{TO_DETAIL_VIEW}}({{argRowObject}}, '{{(context.Config.MultiViewDetailLinkBehavior == Config.E_MultiViewDetailLinkBehavior.NavigateToReadOnlyMode ? "readonly" : "edit")}}')} className="text-color-link whitespace-nowrap px-1">詳細</button>
                        )
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
                    import { VForm2 } from '../../collection'
                    import * as AggregateType from '../../autogenerated-types'
                    import * as AggregateHook from '../../autogenerated-hooks'
                    import * as RefTo from '../../ref-to'
                    import { {{UiContext.CONTEXT_NAME}} } from '../../default-ui-component'

                    export default function () {
                      const [, dispatchMsg] = Util.useMsgContext()
                      const [, dispatchToast] = Util.useToastContext()

                      // 検索処理
                      const { {{LoadMethod.LOAD}}, {{LoadMethod.COUNT}}, {{LoadMethod.CURRENT_PAGE_ITEMS}} } = AggregateHook.{{loadMethod.ReactHookName}}(true)
                      const [defaultSearchCondition, setDefaultSearchCondition] = React.useState(AggregateType.{{searchCondition.CreateNewObjectFnName}})
                      const [totalItemCount, setTotalItemCount] = React.useState(0)
                      const [ready, setReady] = React.useState(false) // 初期表示処理が終わったかどうか
                      const [defaultCollapse, setDefaultCollapse] = React.useState(false) // 初期表示時に検索条件欄を閉じるかどうか

                      const executeSearch = useEvent(async (searchCondition: AggregateType.{{searchCondition.TsTypeName}}) => {
                        setReady(false)
                        try {
                          setDefaultSearchCondition(searchCondition)
                          await Promise.all([
                            {{LoadMethod.COUNT}}(searchCondition.{{SearchCondition.FILTER_TS}}).then(setTotalItemCount),
                            {{LoadMethod.LOAD}}(searchCondition),
                          ])
                        } finally {
                          setReady(true)
                        }
                      })

                      // 画面初期表示時
                      const { search: locationSearch } = useLocation()
                      useEffect(() => {
                        setDefaultCollapse(!!locationSearch) // URLで検索条件が指定されている場合、わざわざ画面上の検索条件欄に入力することが少ないため、検索条件欄を閉じる
                        const condition = AggregateType.{{searchCondition.ParseQueryParameter}}(locationSearch)
                        executeSearch(condition)
                      }, [locationSearch])

                      // 再検索やページングによる読み込み直し
                      const navigateToThis = AggregateHook.{{NavigationHookName}}()
                      const handleReload = useEvent((searchCondition: AggregateType.{{searchCondition.TsTypeName}}) => {
                        setDefaultCollapse(true)
                        navigateToThis(searchCondition) // URL更新
                        executeSearch(searchCondition) // 再検索
                      })

                      // カスタマイズ
                      const { {{UiContextSectionName}}: UI, {{CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})

                      // ブラウザのタイトル
                      const browserTitle = React.useMemo(() => {
                        return UI.{{SET_BROWSER_TITLE}}?.() ?? '{{_aggregate.Item.DisplayName.Replace("'", "\\'")}}'
                      }, [UI.{{SET_BROWSER_TITLE}}])

                      return ready ? (
                        <AfterLoaded
                          defaultSearchCondition={defaultSearchCondition}
                          totalItemCount={totalItemCount}
                          currentPageItems={{{LoadMethod.CURRENT_PAGE_ITEMS}}}
                          reload={handleReload}
                          browserTitle={browserTitle}
                          defaultCollapse={defaultCollapse}
                        />
                      ) : (
                        <Input.NowLoading />
                      )
                    }

                    const AfterLoaded = ({ defaultSearchCondition, totalItemCount, currentPageItems, reload, browserTitle, defaultCollapse }: {
                      /** 最後に検索した時の検索条件 */
                      defaultSearchCondition: AggregateType.{{searchCondition.TsTypeName}}
                      /** 最後に検索した時の検索条件での検索結果総数 */
                      totalItemCount: number
                      /** 最後に検索した時の検索条件での検索結果 */
                      currentPageItems: AggregateType.{{searchResult.TsTypeName}}[]
                      /** 再検索やページングによる読み込み直し */
                      reload: (searchCondition: AggregateType.{{searchCondition.TsTypeName}}) => void
                      /** ブラウザのタイトル */
                      browserTitle: string
                      /** 初期表示時に検索条件欄が閉じられているかどうか */
                      defaultCollapse: boolean
                    }) => {
                      const [, dispatchMsg] = Util.useMsgContext()
                      const [, dispatchToast] = Util.useToastContext()

                      // 初期表示時に検索条件欄を閉じるかどうか
                      React.useEffect(() => {
                        if (defaultCollapse) searchConditionPanelRef.current?.collapse()
                      }, [])

                      // 検索条件
                      const rhfSearchMethods = Util.useFormEx<AggregateType.{{searchCondition.TsTypeName}}>({ defaultValues: defaultSearchCondition })
                      const {
                        getValues: getConditionValues,
                        registerEx: registerExCondition,
                        reset: resetSearchCondition,
                        formState: { defaultValues }, // 最後に検索した時の検索条件
                        control,
                        handleSubmit,
                      } = rhfSearchMethods

                      // 検索条件の並び順コンボボックス
                      const {{SORT_COMBO_FILTERING}} = useEvent((keyword: string | undefined) => {
                        // 既に選択されている選択肢を除外する。
                        // 同じ項目の「昇順」「降順」はどちらか片方のみ選択可能なので、末尾の昇順降順を除いた値で判定する
                        const selected = new Set(getConditionValues('{{SearchCondition.SORT_TS}}')?.map(x => x.replace(/({{SearchCondition.ASC_SUFFIX}}|{{SearchCondition.DESC_SUFFIX}})$/, '')))
                        const notSelected = SORT_COMBO_SOURCE.filter(x => {
                          const cleanedOption = x.replace(/({{SearchCondition.ASC_SUFFIX}}|{{SearchCondition.DESC_SUFFIX}})$/, '')
                          return !selected.has(cleanedOption)
                        })
                        const filtered = keyword ? notSelected.filter(x => x.includes(keyword)) : notSelected
                        return Promise.resolve(filtered)
                      })

                      // 検索条件欄の開閉
                      const searchConditionPanelRef = useRef<ImperativePanelHandle>(null)
                      const [collapsed, setCollapsed] = useState(false)
                      const handleCollapse = useEvent(() => setCollapsed(false))
                      const handleExpand = useEvent(() => setCollapsed(true))
                      const toggleSearchCondition = useCallback(() => {
                        if (searchConditionPanelRef.current?.isCollapsed()) {
                          searchConditionPanelRef.current.expand()
                        } else {
                          searchConditionPanelRef.current?.collapse()
                        }
                      }, [searchConditionPanelRef])

                      // ページング
                      const paging = Input.usePager(
                        defaultValues?.skip,
                        defaultValues?.take,
                        totalItemCount,
                        skip => reload({ ...getConditionValues(), skip }))

                      // クリア時処理
                      const clearSearchCondition = useEvent(() => {
                        resetSearchCondition(AggregateType.{{searchCondition.CreateNewObjectFnName}}())
                        searchConditionPanelRef.current?.expand()
                      })

                      // 画面遷移（詳細画面）
                      const {{TO_DETAIL_VIEW}} = Util.{{singleView.GetNavigateFnName(SingleView.E_Type.ReadOnly)}}()
                      const navigateToCreateView = Util.{{singleView.GetNavigateFnName(SingleView.E_Type.New)}}()
                      const onClickCreateViewLink = useEvent(() => navigateToCreateView())

                      // 画面遷移（一括編集画面）
                      const {{TO_MULTI_EDIT_VIEW}} = AggregateHook.{{multiEditView.NavigationHookName}}()
                      const onClickMultiEditViewLink = useEvent(() => {
                        // undefinedを許容するかどうかで型エラーが出るがこの時点での検索条件は必ず何かしら指定されているはずなのでキャストする
                        {{TO_MULTI_EDIT_VIEW}}(defaultValues as AggregateType.{{searchCondition.TsTypeName}})
                      })

                      // カスタマイズ
                      const { {{UiContextSectionName}}: UI, {{CellType.USE_HELPER}}, ...Components } = React.useContext({{UiContext.CONTEXT_NAME}})
                      const tableRef = useRef<Layout.DataTableRef<AggregateType.{{searchResult.TsTypeName}}>>(null)
                      const getSelectedItems = useEvent(() => {
                        return tableRef.current?.getSelectedRows().map(x => x.row) ?? []
                      })
                      const columnCustomizer = UI.{{SEARCH_RESULT_CUSTOMIZER}}?.()

                      // 列定義
                      const cellType = {{CellType.USE_HELPER}}<AggregateType.{{searchResult.TsTypeName}}>()
                      const columnDefs: Layout.DataTableColumn<AggregateType.{{searchResult.TsTypeName}}>[] = useMemo(() => {
                        const defs: Layout.DataTableColumn<AggregateType.{{searchResult.TsTypeName}}>[] = [
                          {{WithIndent(tableBuilder.RenderColumnDef(context), "      ")}}
                        ]
                        return columnCustomizer?.(defs) ?? defs
                      }, [{{TO_DETAIL_VIEW}}, columnCustomizer, cellType])

                      return (
                        <Layout.PageFrame
                          browserTitle={browserTitle}
                          header={<>
                            <Layout.PageTitle className="self-center">
                              {{_aggregate.Item.DisplayName}}
                            </Layout.PageTitle>
                            <div className="flex-1"></div>
                    {{If(!_aggregate.Item.Options.IsReadOnlyAggregate, () => $$"""
                            <Input.IconButton className="self-center" onClick={onClickMultiEditViewLink}>一括編集</Input.IconButton>
                            <Input.IconButton className="self-center" onClick={onClickCreateViewLink}>新規作成</Input.IconButton>
                    """)}}
                            {UI.{{HEADER_CUSTOMIZER}} && (
                              <UI.{{HEADER_CUSTOMIZER}} getSelectedItems={getSelectedItems} />
                            )}
                            <Input.IconButton className="self-center" onClick={clearSearchCondition}>クリア</Input.IconButton>
                            <div className="self-center flex">
                              <Input.IconButton submit form="search-condition-form" icon={Icon.MagnifyingGlassIcon} fill>検索</Input.IconButton>
                              <div className="self-stretch w-px bg-color-base"></div>
                              <Input.IconButton icon={collapsed ? Icon.ChevronDownIcon : Icon.ChevronUpIcon} fill onClick={toggleSearchCondition} hideText>検索条件</Input.IconButton>
                            </div>
                          </>}
                        >

                          <PanelGroup direction="vertical">

                            {/* 検索条件欄 */}
                            <Panel ref={searchConditionPanelRef} defaultSize={30} collapsible onCollapse={handleCollapse} onExpand={handleExpand} className="max-h-max">
                              <form id="search-condition-form" onSubmit={handleSubmit(reload)} className="h-full overflow-y-scroll border border-color-4 bg-color-gutter">
                                <FormProvider {...rhfSearchMethods}>
                                  {UI.{{SEARCH_CONDITION_CUSTOMIZER}} ? (
                                    <UI.{{SEARCH_CONDITION_CUSTOMIZER}} />
                                  ) : (
                                    {{WithIndent(searchCondition.RenderVForm2(pageRenderingContext, true), "                ")}}
                                  )}
                                </FormProvider>
                              </form>
                            </Panel>

                            <PanelResizeHandle className="h-2" />

                            {/* 検索結果欄 */}
                            <Panel className="flex flex-col gap-1">
                              <Layout.DataTable
                                ref={tableRef}
                                data={{{LoadMethod.CURRENT_PAGE_ITEMS}}}
                                columns={columnDefs}
                                className="flex-1 border border-color-4"
                              />
                              <Input.ServerSidePager {...paging} className="self-center" />
                            </Panel>
                          </PanelGroup>

                        </Layout.PageFrame>
                      )
                    }

                    /** ページ件数のコンボボックスの設定 */
                    const {{PAGE_SIZE_COMBO_SETTING}} = {
                      onFilter: (keyword: string | undefined) => Promise.resolve([20, 50, 100]),
                      getOptionText: (opt: number) => `${opt}件`,
                      getValueText: (value: number) => `${value}件`,
                      getValueFromOption: (opt: number) => opt,
                    }

                    /** 初期並び順のコンボボックスの設定 */
                    const {{SORT_COMBO_SETTING}} = {
                      getOptionText: (opt: typeof SORT_COMBO_SOURCE[0]) => opt,
                      getValueText: (value: typeof SORT_COMBO_SOURCE[0]) => value,
                      getValueFromOption: (opt: typeof SORT_COMBO_SOURCE[0]) => opt,
                    }
                    /** 初期並び順のコンボボックスのデータソース */
                    {{If(searchCondition.GetSortLiterals().Any(), () => $$"""
                    const SORT_COMBO_SOURCE = [
                    {{searchCondition.GetSortLiterals().SelectTextTemplate(sort => $$"""
                      '{{sort}}' as const,
                    """)}}
                    ]
                    """).Else(() => $$"""
                    const SORT_COMBO_SOURCE: string[] = []
                    """)}}
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
                      searchParams.append('{{SearchCondition.URL_FILTER}}', JSON.stringify(init.{{SearchCondition.FILTER_TS}}))
                      if (init.{{SearchCondition.KEYWORD_TS}}) searchParams.append('{{SearchCondition.URL_KEYWORD}}', init.{{SearchCondition.KEYWORD_TS}})
                      if (init.{{SearchCondition.SORT_TS}} && init.{{SearchCondition.SORT_TS}}.length > 0) searchParams.append('{{SearchCondition.URL_SORT}}', JSON.stringify(init.{{SearchCondition.SORT_TS}}))
                      if (init.{{SearchCondition.TAKE_TS}} !== undefined) searchParams.append('{{SearchCondition.URL_TAKE}}', init.{{SearchCondition.TAKE_TS}}.toString())
                      if (init.{{SearchCondition.SKIP_TS}} !== undefined) searchParams.append('{{SearchCondition.URL_SKIP}}', init.{{SearchCondition.SKIP_TS}}.toString())
                    }

                    navigate({
                      pathname: '{{Url}}',
                      search: searchParams.toString()
                    })
                  }, [navigate])
                }
                """;
        }

        #region カスタマイズ部分
        private const string SET_BROWSER_TITLE = "setBrowserTitle";
        private const string HEADER_CUSTOMIZER = "HeaderComponent";
        private const string SEARCH_CONDITION_CUSTOMIZER = "SearchConditionComponent";
        private const string SEARCH_RESULT_CUSTOMIZER = "useGridColumnCustomizer";

        internal void RegisterUiContext(UiContext uiContext) {
            var searchResult = new DataClassForDisplay(_aggregate);
            uiContext.Add($$"""
                import * as {{UiContextSectionName}} from './{{ReactProject.PAGES}}/{{DirNameInPageDir}}/multi-view'
                """, $$"""
                /** {{_aggregate.Item.DisplayName.Replace("*/", "")}} の一覧検索画面 */
                {{UiContextSectionName}}: {
                  /** 詳細画面全体 */
                  default: () => React.ReactNode
                  /** この画面を表示したときのブラウザのタイトルを編集します。 */
                  {{SET_BROWSER_TITLE}}?: () => string
                  /** ヘッダ部分（画面タイトルや検索ボタンがあるあたり）に追加されます。 */
                  {{HEADER_CUSTOMIZER}}?: (props: {
                    getSelectedItems: (() => {{searchResult.TsTypeName}}[])
                  }) => React.ReactNode
                  /** 検索条件欄。これが指定されている場合、自動生成された検索条件欄は使用されません。 */
                  {{SEARCH_CONDITION_CUSTOMIZER}}?: () => React.ReactNode
                  /** 検索結果欄のグリッドの列定義を編集するReactフックを返してください。 */
                  {{SEARCH_RESULT_CUSTOMIZER}}?: () => ((defaultColumns: Layout.DataTableColumn<{{searchResult.TsTypeName}}>[]) => Layout.DataTableColumn<{{searchResult.TsTypeName}}>[])
                }
                """, $$"""
                {{UiContextSectionName}}: {
                  default: {{UiContextSectionName}}.default,
                  {{SET_BROWSER_TITLE}}: undefined,
                  {{HEADER_CUSTOMIZER}}: undefined,
                  {{SEARCH_CONDITION_CUSTOMIZER}}: undefined,
                  {{SEARCH_RESULT_CUSTOMIZER}}: undefined,
                }
                """);
        }
        #endregion カスタマイズ部分

        #region URL取得
        internal const string GET_URL_FROM_DISPLAY_DATA = "GetMultiViewUrlFromDisplayData";
        /// <summary>
        /// 画面のURLを貰えるApplicationServiceのメソッド
        /// </summary>
        internal string RenderAppSrvGetUrlMethod() {

            return $$"""
                /// <summary>
                /// 一覧検索画面を表示するためのURLを返します。
                /// URLにドメイン部分は含まれません。
                /// </summary>
                public virtual string {{GET_URL_FROM_DISPLAY_DATA}}{{_aggregate.Item.PhysicalName}}() {
                    return $"{{Url}}";
                }
                """;
        }
        #endregion
    }
}
