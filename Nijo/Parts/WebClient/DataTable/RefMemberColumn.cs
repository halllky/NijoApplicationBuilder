using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient.DataTable {
    /// <summary>
    /// 参照メンバのDataTable列（参照先のキーや名前を1つのカラムで表示する場合のもの）
    /// </summary>
    internal class RefMemberColumn : IDataTableColumn2 {
        internal RefMemberColumn(AggregateMember.Ref @ref, IEnumerable<string> pathFromRowObject, DataTableBuilder tableContext) {
            _ref = @ref;
            _pathFromRowObject = pathFromRowObject;
            _tableContext = tableContext;
        }

        private readonly AggregateMember.Ref _ref;
        private readonly IEnumerable<string> _pathFromRowObject;
        private readonly DataTableBuilder _tableContext;

        public string Header => _ref.MemberName;
        public string? HeaderGroupName => _ref.Owner == _tableContext.TableOwner ? null : _ref.Owner.Item.DisplayName;

        public bool Hidden => false;
        public int? DefaultWidth => null;
        public bool EnableResizing => true;


        string IDataTableColumn2.RenderDisplayContents(CodeRenderingContext ctx, string arg, string argRowObject) {
            var names = _ref.RefTo
                .AsEntry()
                .GetNames()
                .OfType<AggregateMember.ValueMember>();
            return $$"""
                {{arg}} => {
                  const value = {{argRowObject}}.{{_pathFromRowObject.Join("?.")}}
                  const formatted = `{{names.Select(n => $"${{value?.{n.Declared.GetFullPathAsDataClassForRefTarget().Join("?.")} ?? ''}}").Join("")}}`

                  return (
                    <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                      {formatted}
                      &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
                    </span>
                  )
                }
                """;
        }

        IGridColumnSetting? IDataTableColumn2.GetEditSetting() {
            var refEntry = _ref.RefTo.GetRefEntryEdge().Terminal;
            var refInfo = new RefDisplayData(_ref.RefTo, refEntry);
            var refSearch = new RefSearchMethod(_ref.RefTo, refEntry);
            var combo = new SearchComboBox(_ref.RefTo);
            var keys = _ref.RefTo
                .AsEntry()
                .GetKeys()
                .OfType<AggregateMember.ValueMember>();
            var names = _ref.RefTo
                .AsEntry()
                .GetNames()
                .OfType<AggregateMember.ValueMember>();

            return new AsyncComboboxColumnSetting {
                OptionItemTypeName = $"AggregateType.{refInfo.TsTypeName}",
                QueryKey = combo.RenderReactQueryKeyString(),
                Query = $$"""
                    async keyword => {
                      const response = await post<AggregateType.{{refInfo.TsTypeName}}[]>(`{{refSearch.Url}}`, { keyword })
                      if (!response.ok) return []
                      return response.data
                    }
                    """,
                EmitValueSelector = $"item => item",
                MatchingKeySelectorFromEmitValue = $"item => item.{RefDisplayData.INSTANCE_KEY_TS}",
                MatchingKeySelectorFromOption = $"item => item.{RefDisplayData.INSTANCE_KEY_TS}",
                TextSelector = $"item => `{names.Select(n => $"${{item.{n.Declared.GetFullPathAsDataClassForRefTarget().Join("?.")} ?? ''}}").Join("")}`",
                OnClipboardCopy = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? JSON.stringify({{value}}) : ''
                    """,
                OnClipboardPaste = (value, formatted) => $$"""
                    let {{formatted}}: AggregateType.{{refInfo.TsTypeName}} | undefined
                    if ({{value}}) {
                      try {
                        const obj: AggregateType.{{refInfo.TsTypeName}} = JSON.parse({{value}})
                        // 登録にはインスタンスキーが使われるのでキーの型だけは細かくチェックする
                        if (obj.{{RefDisplayData.INSTANCE_KEY_TS}} === undefined) throw new Error
                        const arrInstanceKey: [{{keys.Select(k => k.Options.MemberType.GetTypeScriptTypeName()).Join(", ")}}] = JSON.parse(obj.{{RefDisplayData.INSTANCE_KEY_TS}})
                        if (!Array.isArray(arrInstanceKey)) throw new Error
                    {{keys.SelectTextTemplate((k, i) => $$"""
                        if (typeof arrInstanceKey[{{i}}] !== '{{k.Options.MemberType.GetTypeScriptTypeName()}}') throw new Error
                    """)}}
                        {{formatted}} = obj
                      } catch {
                        {{formatted}} = undefined
                      }
                    } else {
                      {{formatted}} = undefined
                    }
                    """,
            };
        }

        string IDataTableColumn2.RenderGetterOnEditStart(CodeRenderingContext ctx) {
            return $$"""
                row => row.{{_pathFromRowObject.Join("?.")}}
                """;
        }

        string IDataTableColumn2.RenderSetterOnEditEnd(CodeRenderingContext ctx) {
            return $$"""
                (row, value) => {
                  row.{{_pathFromRowObject.Join(".")}} = value
                }
                """;
        }

        string IDataTableColumn2.RenderOnClipboardCopy(CodeRenderingContext ctx) {
            var editSetting = (AsyncComboboxColumnSetting)((IDataTableColumn2)this).GetEditSetting()!;
            return $$"""
                row => {
                  {{WithIndent(editSetting.OnClipboardCopy($"row.{_pathFromRowObject?.Join("?.")}", "formatted"), "  ")}}
                  return formatted
                }
                """;
        }

        string IDataTableColumn2.RenderOnClipboardPaste(CodeRenderingContext ctx) {
            var editSetting = (AsyncComboboxColumnSetting)((IDataTableColumn2)this).GetEditSetting()!;
            return $$"""
                (row, value) => {
                {{If(_pathFromRowObject.Count() >= 2, () => $$"""
                  if (row.{{_pathFromRowObject.SkipLast(1).Join("?.")}} === undefined) return
                """)}}
                  {{WithIndent(editSetting.OnClipboardPaste("value", "formatted"), "  ")}}
                  row.{{_pathFromRowObject.Join(".")}} = formatted
                }
                """;
        }
    }
}
