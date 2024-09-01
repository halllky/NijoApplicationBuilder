using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient.DataTable {
    /// <summary>
    /// 値型のDataTable列
    /// </summary>
    internal class ValueMemberColumn : IDataTableColumn2 {
        internal ValueMemberColumn(AggregateMember.ValueMember vm, IEnumerable<string> pathFromRowObject, DataTableBuilder tableContext) {
            _vm = vm;
            _pathFromRowObject = pathFromRowObject;
            _tableContext = tableContext;
        }

        private readonly AggregateMember.ValueMember _vm;
        private readonly IEnumerable<string> _pathFromRowObject;
        private readonly DataTableBuilder _tableContext;

        public string Header => _vm.MemberName;
        public string? HeaderGroupName => _vm.Owner == _tableContext.TableOwner ? null : _vm.Owner.Item.DisplayName;

        public bool Hidden => _vm.Options.InvisibleInGui;
        public int? DefaultWidth => null;
        public bool EnableResizing => true;

        IGridColumnSetting? IDataTableColumn2.GetEditSetting() {
            return _vm.Options.MemberType.GetGridColumnEditSetting();
        }
        string IDataTableColumn2.RenderDisplayContents(CodeRenderingContext ctx, string arg, string argRowObject) {
            string? formatted = null;
            var editSetting = _vm.Options.MemberType.GetGridColumnEditSetting();
            if (editSetting is ComboboxColumnSetting ccs) {
                formatted = ccs.GetDisplayText?.Invoke("value", "formatted");
            }

            return $$"""
                {{arg}} => {
                  const value = {{argRowObject}}.{{_pathFromRowObject.Join("?.")}}
                  {{If(formatted != null, () => WithIndent(formatted!, "  "))}}
                  return (
                    <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                      {{{(formatted == null ? "value" : "formatted")}}}
                      &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
                    </span>
                  )
                }
                """;
        }

        string IDataTableColumn2.RenderGetterOnEditStart(CodeRenderingContext ctx) {
            var editSetting = _vm.Options.MemberType.GetGridColumnEditSetting();

            return editSetting?.GetValueFromRow == null ? $$"""
                row => row.{{_pathFromRowObject.Join("?.")}}
                """ : $$"""
                row => {
                  const value = row.{{_pathFromRowObject.Join("?.")}}
                  {{WithIndent(editSetting.GetValueFromRow("value", "formatted"), "  ")}}
                  return formatted
                }
                """;
        }

        string IDataTableColumn2.RenderSetterOnEditEnd(CodeRenderingContext ctx) {
            var editSetting = _vm.Options.MemberType.GetGridColumnEditSetting();

            if (_vm.DeclaringAggregate == _tableContext.TableOwner) {
                return $$"""
                    (row, value) => {
                    {{If(editSetting?.SetValueToRow == null, () => $$"""
                      row.{{_pathFromRowObject.Join(".")}} = value
                    """).Else(() => $$"""
                      {{WithIndent(editSetting!.SetValueToRow!("value", "formatted"), "  ")}}
                      row.{{_pathFromRowObject.Join(".")}} = formatted
                    """)}}
                    }
                    """;
            } else {
                return $$"""
                    (row, value) => {
                      if (row.{{_pathFromRowObject.SkipLast(1).Join("?.")}}) {
                    {{If(editSetting?.SetValueToRow == null, () => $$"""
                        row.{{_pathFromRowObject.Join(".")}} = value
                    """).Else(() => $$"""
                        {{WithIndent(editSetting!.SetValueToRow!("value", "formatted"), "    ")}}
                        row.{{_pathFromRowObject.Join(".")}} = formatted
                    """)}}
                      }
                    }
                    """;
            }
        }

        string IDataTableColumn2.RenderOnClipboardCopy(CodeRenderingContext ctx) {
            var editSetting = _vm.Options.MemberType.GetGridColumnEditSetting();
            var onCopy = editSetting switch {
                TextColumnSetting => throw new NotImplementedException(), // TextColumnSettingの場合はクリップボード処理が呼ばれないので
                ComboboxColumnSetting combo => combo.OnClipboardCopy,
                AsyncComboboxColumnSetting asyncCombo => asyncCombo.OnClipboardCopy,
                _ => throw new NotImplementedException(),
            };

            return $$"""
                row => {
                  {{WithIndent(onCopy($"row.{_pathFromRowObject?.Join("?.")}", "formatted"), "  ")}}
                  return formatted
                }
                """;
        }

        string IDataTableColumn2.RenderOnClipboardPaste(CodeRenderingContext ctx) {
            var editSetting = _vm.Options.MemberType.GetGridColumnEditSetting();
            var onPaste = editSetting switch {
                TextColumnSetting => throw new NotImplementedException(), // TextColumnSettingの場合はクリップボード処理が呼ばれないので
                ComboboxColumnSetting combo => combo.OnClipboardPaste,
                AsyncComboboxColumnSetting asyncCombo => asyncCombo.OnClipboardPaste,
                _ => throw new NotImplementedException(),
            };

            return $$"""
                (row, value) => {
                {{If(_pathFromRowObject.Count() >= 2, () => $$"""
                  if (row.{{_pathFromRowObject.SkipLast(1).Join("?.")}} === undefined) return
                """)}}
                  {{WithIndent(onPaste("value", "formatted"), "  ")}}
                  row.{{_pathFromRowObject.Join(".")}} = formatted
                }
                """;
        }
    }
}
