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
    /// テーブル列定義ビルダー
    /// </summary>
    internal class DataTableBuilder {
        internal DataTableBuilder(GraphNode<Aggregate> tableOwner, string rowTypeName, bool editable) {
            TableOwner = tableOwner;
            _rowTypeName = rowTypeName;
            _editable = editable;
        }

        /// <summary>
        /// どの集約のメンバーのテーブルか
        /// </summary>
        internal GraphNode<Aggregate> TableOwner { get; }
        private readonly string _rowTypeName;
        private readonly bool _editable;
        private readonly List<IDataTableColumn2> _columns = [];

        /// <summary>
        /// 列を追加します。
        /// </summary>
        internal DataTableBuilder Add(IDataTableColumn2 column) {
            _columns.Add(column);
            return this;
        }

        #region AddMembers
        /// <summary>
        /// 画面表示用データのメンバーの列を追加します。
        /// </summary>
        internal DataTableBuilder AddMembers(DataClassForDisplay dataClass) {
            _columns.AddRange(Enumerate(dataClass));
            return this;

            IEnumerable<IDataTableColumn2> Enumerate(DataClassForDisplay rendering) {
                foreach (var member in rendering.GetOwnMembers()) {
                    if (member is AggregateMember.ValueMember vm) {
                        if (vm.DeclaringAggregate != rendering.Aggregate) continue;
                        if (vm.Options.InvisibleInGui) continue;
                        var column = new ValueMemberColumn(
                            vm,
                            vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, since: TableOwner),
                            this);
                        yield return column;

                    } else if (member is AggregateMember.Ref @ref) {
                        // RefをChildと同じように複数列に分けて表示する
                        var desc = new DataClassForDisplayDescendant(@ref);
                        foreach (var recursive in Enumerate(desc)) {
                            yield return recursive;
                        }

                        //// Refのキーと名前を1つのカラムで表示する
                        //var column = new RefMemberColumn(
                        //    @ref,
                        //    @ref.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, since: TableOwner),
                        //    this);
                        //yield return column;
                    }
                }
                foreach (var desc in rendering.GetChildMembers()) {

                    // ChildrenやVariationのメンバーを列挙していないのはグリッド上で表現できないため
                    if (desc.MemberInfo is AggregateMember.Children) continue;
                    if (desc.MemberInfo is AggregateMember.VariationItem) continue;

                    foreach (var reucusive in Enumerate(desc)) {
                        yield return reucusive;
                    }
                }
            }
        }

        /// <summary>
        /// 参照先の画面表示用データのメンバーの列を追加します。
        /// </summary>
        internal DataTableBuilder AddMembers(RefDisplayData refDisplayData) {
            _columns.AddRange(Enumerate(refDisplayData));
            return this;

            IEnumerable<IDataTableColumn2> Enumerate(RefDisplayData rendering) {
                foreach (var member in rendering.GetOwnMembers()) {
                    if (member is AggregateMember.ValueMember vm) {
                        if (vm.Options.InvisibleInGui) continue;
                        var column = new ValueMemberColumn(
                            vm,
                            vm.Declared.GetFullPathAsDataClassForRefTarget(since: TableOwner),
                            this);
                        yield return column;

                    } else if (member is AggregateMember.Ref @ref) {
                        // RefをChildと同じように複数列に分けて表示する
                        var desc = new RefDisplayDataDescendant(@ref, refDisplayData._refEntry);
                        foreach (var recursive in Enumerate(desc)) {
                            yield return recursive;
                        }

                        //// Refのキーと名前を1つのカラムで表示する
                        //var column = new RefMemberColumn(
                        //    @ref,
                        //    @ref.GetFullPathAsDataClassForRefTarget(since: TableOwner),
                        //    this);
                        //yield return column;
                    }
                }
                foreach (var desc in rendering.GetChildMembers()) {

                    // ChildrenやVariationのメンバーを列挙していないのはグリッド上で表現できないため
                    if (desc.MemberInfo is AggregateMember.Children) continue;
                    if (desc.MemberInfo is AggregateMember.VariationItem) continue;

                    foreach (var reucusive in Enumerate(desc)) {
                        yield return reucusive;
                    }
                }
            }
        }

        /// <summary>
        /// コマンドパラメータのメンバーの列を追加します。
        /// </summary>
        internal DataTableBuilder AddMembers(Models.CommandModelFeatures.CommandParameter commandParameter) {
            _columns.AddRange(Enumerate(commandParameter));
            return this;

            IEnumerable<IDataTableColumn2> Enumerate(Models.CommandModelFeatures.CommandParameter rendering) {
                var members = rendering
                    .GetOwnMembers()
                    .Select(m => m.MemberInfo)
                    .ToArray();
                foreach (var member in members) {
                    if (member is AggregateMember.ValueMember vm) {
                        if (vm.Options.InvisibleInGui) continue;
                        var column = new ValueMemberColumn(
                            vm,
                            vm.Declared.GetFullPathAsDataClassForRefTarget(since: TableOwner),
                            this);
                        yield return column;

                    } else if (member is AggregateMember.Ref @ref) {
                        // RefをChildと同じように複数列に分けて表示する
                        var childParam = new Models.CommandModelFeatures.CommandParameter.Member(@ref);
                        foreach (var reucusive in Enumerate(childParam.GetMemberParameter()!)) {
                            yield return reucusive;
                        }

                        //// Refのキーと名前を1つのカラムで表示する
                        //var column = new RefMemberColumn(
                        //    @ref,
                        //    @ref.GetFullPathAsDataClassForRefTarget(since: TableOwner),
                        //    this);
                        //yield return column;

                    } else if (member is AggregateMember.Child child) {
                        var childParam = new Models.CommandModelFeatures.CommandParameter.Member(child);
                        foreach (var reucusive in Enumerate(childParam.GetMemberParameter()!)) {
                            yield return reucusive;
                        }
                    }
                    // ChildrenやVariationのメンバーを列挙していないのはグリッド上で表現できないため
                }
            }
        }
        #endregion AddMembers

        internal string RenderColumnDef(CodeRenderingContext context) {
            string Render(IDataTableColumn2 column, int index) {
                var editSetting = column.GetEditSetting();
                var textboxEditSetting = editSetting as TextColumnSetting;
                var comboboxEditSetting = editSetting as ComboboxColumnSetting;
                var asyncComboEditSetting = editSetting as AsyncComboboxColumnSetting;

                return $$"""
                    {
                      id: 'col-{{index}}',
                      header: '{{column.Header}}',
                      cell: {{WithIndent(column.RenderDisplayContents(context, "cellProps", "cellProps.row.original"), "  ")}},
                    {{If(column.DefaultWidth != null, () => $$"""
                      size: {{column.DefaultWidth}},
                    """)}}
                    {{If(column.EnableResizing, () => $$"""
                      enableResizing: true,
                    """)}}
                    {{If(column.Hidden, () => $$"""
                      hidden: true,
                    """)}}
                    {{If(column.HeaderGroupName != null, () => $$"""
                      headerGroupName: '{{column.HeaderGroupName}}',
                    """)}}
                    {{If(_editable && textboxEditSetting != null, () => $$"""
                      editSetting: {
                        type: 'text',
                        getTextValue: {{WithIndent(column.RenderGetterOnEditStart(context), "    ")}},
                        setTextValue: {{WithIndent(column.RenderSetterOnEditEnd(context), "    ")}},
                      },
                    """)}}
                    {{If(_editable && comboboxEditSetting != null, () => $$"""
                      editSetting: (() => {
                        const comboSetting: Layout.ColumnEditSetting<{{_rowTypeName}}, {{comboboxEditSetting!.OptionItemTypeName}}> = {
                          type: 'combo',
                          getValueFromRow: {{WithIndent(column.RenderGetterOnEditStart(context), "      ")}},
                          setValueToRow: {{WithIndent(column.RenderSetterOnEditEnd(context), "      ")}},
                          onClipboardCopy: {{WithIndent(column.RenderOnClipboardCopy(context), "      ")}},
                          onClipboardPaste: {{WithIndent(column.RenderOnClipboardPaste(context), "      ")}},
                          comboProps: {
                            options: {{comboboxEditSetting!.Options}},
                            emitValueSelector: {{comboboxEditSetting!.EmitValueSelector}},
                            matchingKeySelectorFromEmitValue: {{comboboxEditSetting!.MatchingKeySelectorFromEmitValue}},
                            matchingKeySelectorFromOption: {{comboboxEditSetting!.MatchingKeySelectorFromOption}},
                            textSelector: {{comboboxEditSetting!.TextSelector}},
                          },
                        }
                        return comboSetting as Layout.ColumnEditSetting<{{_rowTypeName}}, unknown>
                      })(),
                    """)}}
                    {{If(_editable && asyncComboEditSetting != null, () => $$"""
                      editSetting: (() => {
                        const asyncComboSetting: Layout.ColumnEditSetting<{{_rowTypeName}}, {{asyncComboEditSetting!.OptionItemTypeName}}> = {
                          type: 'async-combo',
                          getValueFromRow: {{WithIndent(column.RenderGetterOnEditStart(context), "      ")}},
                          setValueToRow: {{WithIndent(column.RenderSetterOnEditEnd(context), "      ")}},
                          onClipboardCopy: {{WithIndent(column.RenderOnClipboardCopy(context), "      ")}},
                          onClipboardPaste: {{WithIndent(column.RenderOnClipboardPaste(context), "      ")}},
                          comboProps: {
                            queryKey: {{asyncComboEditSetting!.QueryKey}},
                            query: {{WithIndent(asyncComboEditSetting!.Query, "        ")}},
                            emitValueSelector: {{asyncComboEditSetting!.EmitValueSelector}},
                            matchingKeySelectorFromEmitValue: {{asyncComboEditSetting!.MatchingKeySelectorFromEmitValue}},
                            matchingKeySelectorFromOption: {{asyncComboEditSetting!.MatchingKeySelectorFromOption}},
                            textSelector: {{asyncComboEditSetting!.TextSelector}},
                          },
                        }
                        return asyncComboSetting as Layout.ColumnEditSetting<{{_rowTypeName}}, unknown>
                      })(),
                    """)}}
                    }
                    """;
            }

            return _columns.SelectTextTemplate((col, i) => $$"""
                {{Render(col, i)}},
                """);
        }
    }
}
