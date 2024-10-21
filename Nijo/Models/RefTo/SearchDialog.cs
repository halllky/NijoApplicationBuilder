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

namespace Nijo.Models.RefTo {
    /// <summary>
    /// 参照先データ検索用検索ダイアログ
    /// </summary>
    internal class SearchDialog {
        internal SearchDialog(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            _aggregate = agg;
            _refEntry = refEntry;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<Aggregate> _refEntry;

        internal string HookName => $"use{_aggregate.Item.PhysicalName}SearchDialog";

        internal string RenderHook(CodeRenderingContext context) {
            var searchCondition = new RefSearchCondition(_aggregate, _refEntry);
            var searchResult = new RefDisplayData(_aggregate, _refEntry);
            var search = new RefSearchMethod(_aggregate, _refEntry);

            var pageRenderingContext = new FormUIRenderingContext {
                CodeRenderingContext = context,
                Register = "registerExCondition",
                GetReactHookFormFieldPath = vm => vm.GetFullPathAsRefSearchConditionFilter(E_CsTs.TypeScript),
                RenderReadOnlyStatement = vm => string.Empty, // 検索条件欄の項目が読み取り専用になることはない
                RenderErrorMessage = vm => throw new InvalidOperationException("検索条件欄では項目ごとにエラーメッセージを表示するという概念が無い"),
            };
            var tableBuilder = new DataTableBuilder(_aggregate, $"Types.{searchResult.TsTypeName}", false, _ => "() => {}")
                // 行ヘッダ（選択チェックボックス or 選択ボタン）
                .Add(new AdhocColumn {
                    Header = string.Empty,
                    DefaultWidth = 64,
                    EnableResizing = false,
                    CellContents = (ctx, arg, argRowObject) => $$"""
                        {{arg}} => {
                          const row = {{argRowObject}}
                          return multiSelect ? (
                            // TODO #35 複数選択
                            <Input.CheckBox  />
                          ) : (
                            <Input.IconButton fill mini className="w-full" onClick={() => handleSelectSingle(row)}>選択</Input.IconButton>
                          )
                        }
                        """,
                })
                // メンバーの列
                .AddMembers(searchResult);

            return $$"""
                export const {{HookName}} = () => {
                  const [, dispatch] = Layout.useDialogContext()
                  return useCallback(({ initialSearchCondition, multiSelect, onSelect }: {
                    /** ダイアログを開いた瞬間の検索条件 */
                    initialSearchCondition?: Types.{{searchCondition.TsTypeName}}
                  } & ({
                    /** 複数選択するダイアログならtrue */
                    multiSelect?: false
                    /** 選択確定時処理 */
                    onSelect: (selectedItem: Types.{{searchResult.TsTypeName}} | undefined) => void
                  } | {
                    multiSelect: true
                    onSelect: (selectedItems: Types.{{searchResult.TsTypeName}}[]) => void
                  })) => {
                    dispatch(state => state.pushDialog('{{_aggregate.Item.DisplayName.Replace("'", "\\'")}}検索', ({ closeDialog }) => {

                      // 検索条件
                      const rhfSearchMethods = Util.useFormEx<Types.{{searchCondition.TsTypeName}}>({ defaultValues: initialSearchCondition })
                      const {
                        getValues: getConditionValues,
                        registerEx: registerExCondition,
                        reset: resetSearchCondition,
                        formState: { defaultValues },
                      } = rhfSearchMethods

                      // クリア時処理
                      const clearSearchCondition = useEvent(() => {
                        resetSearchCondition(Types.{{searchCondition.CreateNewObjectFnName}}())
                      })

                      // 検索時処理
                      const { {{RefSearchMethod.LOAD}}, {{RefSearchMethod.COUNT}}, {{RefSearchMethod.CURRENT_PAGE_ITEMS}} } = Hooks.{{search.ReactHookName}}()
                      const reload = useEvent((condition: Types.{{searchCondition.TsTypeName}}) => {
                        resetSearchCondition(condition)
                        {{RefSearchMethod.COUNT}}(condition.filter).then(setTotalItemCount)
                        {{RefSearchMethod.LOAD}}(condition)
                      })
                      const handleReload = useEvent(() => {
                        reload(getConditionValues())
                      })

                      // ページング
                      const [totalItemCount, setTotalItemCount] = useState(0)
                      const pagerState = Input.usePager(
                        defaultValues?.skip,
                        defaultValues?.take,
                        totalItemCount,
                        skip => reload({ ...getConditionValues(), skip }))

                      // 1件選択
                      const handleSelectSingle = (item: Types.{{searchResult.TsTypeName}}) => {
                        if (multiSelect) throw new Error('複数選択モードの場合にこの関数が呼ばれることはあり得ない')
                        onSelect(item)
                        closeDialog()
                      }

                      // 複数選択
                      const dataTableRef = useRef<Layout.DataTableRef<Types.{{searchResult.TsTypeName}}>>(null)
                      const handleSelectMultiple = useEvent(() => {
                        if (!multiSelect) throw new Error('1件選択モードの場合にこの関数が呼ばれることはあり得ない')
                        const selectedItems = dataTableRef.current?.getSelectedRows().map(x => x.row) ?? []
                        onSelect(selectedItems)
                        closeDialog()
                      })

                      // カスタマイズ
                      const {
                        {{Customizer}}: Customizers,
                        {{AutoGeneratedCustomizer.CUSTOM_UI_COMPONENT}},
                      } = {{AutoGeneratedCustomizer.USE_CONTEXT}}()

                      // 検索結果欄の列定義
                      const cellType = Layout.{{CellType.USE_HELPER}}<Types.{{searchResult.TsTypeName}}>()
                      const gridCustomizer = Customizers?.{{SEARCH_RESULT_CUSTOMIZER}}?.()
                      const columnDefs: Layout.DataTableColumn<Types.{{searchResult.TsTypeName}}>[] = useMemo(() => {
                        const defs: Layout.DataTableColumn<Types.{{searchResult.TsTypeName}}>[] = [
                          {{WithIndent(tableBuilder.RenderColumnDef(context), "          ")}}
                        ]
                        return gridCustomizer?.(defs) ?? defs
                      }, [gridCustomizer, cellType])

                      return (
                        <div className="h-full flex flex-col">
                          <PanelGroup direction="vertical" className="flex-1 flex flex-col">

                            {/* 検索条件欄 */}
                            <Panel defaultSize={30} className="flex flex-col">
                              <div className="flex-1 overflow-y-scroll border border-color-4 bg-color-gutter">
                                <FormProvider {...rhfSearchMethods}>
                                  {Customizers?.{{SEARCH_CONDITION_CUSTOMIZER}} ? (
                                    <Customizers.{{SEARCH_CONDITION_CUSTOMIZER}} />
                                  ) : (
                                    {{WithIndent(searchCondition.RenderVForm2(pageRenderingContext, false), "                 ")}}
                                  )}
                                </FormProvider>
                              </div>
                              <div className="flex justify-end gap-2 pt-1">
                                <Input.IconButton outline onClick={clearSearchCondition}>クリア</Input.IconButton>
                                <Input.IconButton fill onClick={handleReload}>検索</Input.IconButton>
                              </div>
                            </Panel>

                            <PanelResizeHandle className="h-2" />

                            {/* 検索結果欄 */}
                            <Panel className="flex flex-col gap-1">
                              <Layout.DataTable
                                data={{{RefSearchMethod.CURRENT_PAGE_ITEMS}}}
                                columns={columnDefs}
                                className="flex-1 border border-color-4"
                              />
                              <Input.ServerSidePager {...pagerState} className="self-center" />
                            </Panel>
                          </PanelGroup>

                          {multiSelect && (
                            <div className="flex justify-end">
                              <Input.IconButton fill onClick={handleSelectMultiple}>選択</Input.IconButton>
                            </div>
                          )}
                        </div>
                      )
                    }))
                  }, [dispatch])

                }
                """;
        }


        #region カスタマイズ部分
        private string Customizer => $"{_aggregate.Item.PhysicalName}SearchDialog";
        private const string SEARCH_CONDITION_CUSTOMIZER = $"SearchConditionComponent";
        private const string SEARCH_RESULT_CUSTOMIZER = $"useGridColumnCustomizer";

        internal string RenderCustomizersDeclaring() {
            var displayData = new RefDisplayData(_aggregate, _refEntry);
            return $$"""
                /** {{_aggregate.Item.DisplayName.Replace("*/", "")}} の検索ダイアログをカスタマイズします。 */
                {{Customizer}}?: {
                  /** 検索条件欄。これが指定されている場合、自動生成された検索条件欄は使用されません。 */
                  {{SEARCH_CONDITION_CUSTOMIZER}}?: () => React.ReactNode
                  /** 検索結果欄のグリッドの列定義を編集するReactフックを返してください。 */
                  {{SEARCH_RESULT_CUSTOMIZER}}?: () => ((defaultColumns: Layout.DataTableColumn<AggregateType.{{displayData.TsTypeName}}>[]) => Layout.DataTableColumn<AggregateType.{{displayData.TsTypeName}}>[])
                }
                """;
        }
        #endregion カスタマイズ部分
    }
}
