using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Parts.Utility;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// <see cref="Parts.WebClient.DataTable.DataTableBuilder"/>
    /// で使われる、参照先の列の列定義ヘルパー関数
    /// </summary>
    internal class DataTableRefColumnHelper {

        internal DataTableRefColumnHelper(GraphNode<Aggregate> refEntry) {
            _refEntry = refEntry;
        }
        private readonly GraphNode<Aggregate> _refEntry;

        internal string MethodName => $"refTo{_refEntry.Item.PhysicalName}";

        internal Parts.WebClient.DataTable.CellType.Helper Render() {
            var keys = _refEntry
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => !vm.Options.InvisibleInGui)
                .ToArray();
            var names = _refEntry
                .GetNames()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => !vm.Options.InvisibleInGui);
            var refColumns = keys.Concat(names).ToArray();

            // フォーカス離脱時の検索
            var keysForSearchOnBlur = keys.Select(vm => {
                // 右辺
                Func<string, string> Right = vm.Options.MemberType is SchalarMemberType
                    ? ((string v) => $"{{ {FromTo.FROM_TS}: {v}, {FromTo.TO_TS}: {v} }}")
                    : ((string v) => v);
                return new {
                    vm.Declared,
                    LeftFullPath = vm.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.TypeScript),
                    Right,
                };
            });

            var displayData = new RefDisplayData(_refEntry, _refEntry);
            var refSearch = new RefSearchMethod(_refEntry, _refEntry);
            var refSearchCondition = new RefSearchCondition(_refEntry, _refEntry);

            var searchDialog = new SearchDialog(_refEntry, _refEntry);

            var use = $$"""
                const { {{RefSearchMethod.LOAD}}: load{{_refEntry.Item.PhysicalName}} } = AggregateHook.{{refSearch.ReactHookName}}(true)
                """;
            var body = $$"""
                /** {{_refEntry.Item.DisplayName}}を参照する列 */
                const {{MethodName}}: {{Parts.WebClient.DataTable.CellType.RETURNS_MANY_COLUMN}}<TRow, AggregateType.{{displayData.TsTypeName}} | undefined> = (header, getValue, setValue, opt) => {
                  const cols: DataTableColumn<TRow>[] = []
                  if (opt?.readOnly !== true) {
                    // 参照先検索の虫眼鏡ダイアログ検索
                    cols.push({
                      id: `${header}::SEARCH-MAG`,
                      render: (row, rowIndex) => {
                        const openDialog = AggregateComponent.{{searchDialog.HookName}}()
                        const handleClick = useEvent(() => {
                          openDialog({
                            onSelect: selectedItem => {
                              setValue(row, selectedItem, rowIndex)
                            }
                          })
                        })
                        if (opt?.readOnly === true) return <></> // 非表示
                        if (typeof opt?.readOnly === 'function' && opt.readOnly(row) === true) return <></> // 非表示
                        return (
                          <Input.IconButton icon={Icon.MagnifyingGlassIcon} onClick={handleClick} className=" m-auto" outline mini hideText>
                            検索
                          </Input.IconButton>
                        )
                      },
                      onClipboardCopy: () => '',
                      fixedWidth: true,
                      defaultWidthPx: 27,
                      headerGroupName: header,
                    })
                  }
                {{refColumns.SelectTextTemplate(vm => $$"""
                  cols.push({{vm.Options.MemberType.DataTableColumnDefHelperName}}('{{vm.MemberName}}',
                    row => getValue(row)?.{{vm.Declared.GetFullPathAsDataClassForRefTarget(since: _refEntry).Join("?.")}},
                    (row, value, rowIndex) => {
                {{If(vm.IsKey, () => $$"""
                      // 変更されたキーで再検索をかける
                      const cond = AggregateType.{{refSearchCondition.CreateNewObjectFnName}}()
                {{keysForSearchOnBlur.SelectTextTemplate(k => $$"""
                {{If(k.Declared == vm.Declared, () => $$"""
                      cond.{{k.LeftFullPath.Join(".")}} = {{k.Right("value")}}
                """).Else(() => $$"""
                      cond.{{k.LeftFullPath.Join(".")}} = {{k.Right($"getValue(row)?.{k.Declared.GetFullPathAsDataClassForRefTarget().Join("?.")}")}}
                """)}}
                """)}}
                      cond.take = 2 // 検索で1件だけヒットしたときのみ値変更
                      load{{_refEntry.Item.PhysicalName}}(cond).then(result => {
                        if (result.length === 0) {
                          setValue(row, undefined, rowIndex)
                        } else if (result.length >= 2) {
                          setValue(row, undefined, rowIndex)
                        } else {
                          setValue(row, result[0], rowIndex)
                        }
                      })
                """)}}
                    }, {
                    ...opt,
                    headerGroupName: header,
                {{If(!vm.IsKey, () => $$"""
                    readOnly: true,
                """)}}
                  }))
                """)}}
                  return cols
                }
                """;

            return new() {
                Uses = [use],
                Body = body,
                Deps = [$"load{_refEntry.Item.PhysicalName}"],
                FunctionName = MethodName,
            };
        }
    }
}