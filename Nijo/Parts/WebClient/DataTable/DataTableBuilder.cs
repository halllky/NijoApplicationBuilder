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
        internal DataTableBuilder(GraphNode<Aggregate> tableOwner, string rowTypeName, bool editable, Func<AggregateMember.AggregateMemberBase, string> onValueChange) {
            TableOwner = tableOwner;
            _rowTypeName = rowTypeName;
            _editable = editable;
            _onValueChange = onValueChange;
        }

        /// <summary>
        /// どの集約のメンバーのテーブルか
        /// </summary>
        internal GraphNode<Aggregate> TableOwner { get; }
        private readonly string _rowTypeName;
        private readonly bool _editable;
        private readonly List<IDataTableColumn2> _columns = [];
        private readonly Func<AggregateMember.AggregateMemberBase, string> _onValueChange;

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
                        // Refのヘルパー関数を呼ぶ
                        var column = new RefMemberColumn(
                            @ref,
                            @ref.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript, since: TableOwner),
                            this);
                        yield return column;

                        //// RefをChildと同じように複数列に分けて表示する
                        //var desc = new DataClassForDisplayDescendant(@ref);
                        //foreach (var recursive in Enumerate(desc)) {
                        //    yield return recursive;
                        //}

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
                        // Refのヘルパー関数を呼ぶ
                        var column = new RefMemberColumn(
                            @ref,
                            @ref.GetFullPathAsDataClassForRefTarget(since: TableOwner),
                            this);
                        yield return column;

                        //// RefをChildと同じように複数列に分けて表示する
                        //var desc = new RefDisplayDataDescendant(@ref, refDisplayData._refEntry);
                        //foreach (var recursive in Enumerate(desc)) {
                        //    yield return recursive;
                        //}

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
                        // Refのヘルパー関数を呼ぶ
                        var column = new RefMemberColumn(
                            @ref,
                            @ref.GetFullPathAsDataClassForRefTarget(since: TableOwner),
                            this);
                        yield return column;

                        //// RefをChildと同じように複数列に分けて表示する
                        //var childParam = new Models.CommandModelFeatures.CommandParameter.Member(@ref);
                        //foreach (var reucusive in Enumerate(childParam.GetMemberParameter()!)) {
                        //    yield return reucusive;
                        //}

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
            return $$"""
                {{_columns.SelectTextTemplate((col, i) => $$"""
                {{WithIndent(Render(col, i), "")}},
                """)}}
                """;

            string Render(IDataTableColumn2 column, int index) {

                if (column is ValueMemberColumn vmColumn) {
                    var helper = vmColumn._vm.Options.MemberType.DataTableColumnDefHelperName;

                    return $$"""
                        cellType.{{helper}}('{{vmColumn.Header.Replace("'", "\\'")}}',
                          r => r.{{vmColumn._pathFromRowObject.Join("?.")}},
                          {{WithIndent(_onValueChange(vmColumn._vm), "  ")}}, {
                        {{If(column.HeaderGroupName != null, () => $$"""
                          headerGroupName: '{{column.HeaderGroupName?.Replace("'", "\\'")}}',
                        """)}}
                        {{If(!_editable, () => $$"""
                          readOnly: true,
                        """)}}
                        {{If(column.DefaultWidth != null, () => $$"""
                          defaultWidthPx: {{column.DefaultWidth}},
                        """)}}
                        {{If(!column.EnableResizing, () => $$"""
                          fixedWidth: true,
                        """)}}
                        })
                        """;
                }

                if (column is RefMemberColumn refColumn) {
                    var helper = new DataTableRefColumnHelper(refColumn._ref.RefTo);

                    return $$"""
                        ...cellType.{{helper.MethodName}}('{{refColumn._ref.DisplayName.Replace("'", "\\'")}}',
                          r => r.{{refColumn._pathFromRowObject.Join("?.")}},
                          {{_onValueChange(refColumn._ref)}}, {
                        {{If(!_editable, () => $$"""
                          readOnly: true,
                        """)}}
                        })
                        """;
                    // readOnly以外のオプションは参照先のどの列にかかるかが不明瞭なので敢えて設定していない
                }

                return $$"""
                    {
                      id: 'col-{{index}}',
                      header: '{{column.Header}}',
                      render: {{WithIndent(column.RenderDisplayContents(context, "r", "r"), "  ")}},
                      onClipboardCopy: {{WithIndent(column.RenderOnClipboardCopy(context), "      ")}},
                    {{If(column.DefaultWidth != null, () => $$"""
                      defaultWidthPx: {{column.DefaultWidth}},
                    """)}}
                    {{If(!column.EnableResizing, () => $$"""
                      fixedWidth: true,
                    """)}}
                    {{If(column.HeaderGroupName != null, () => $$"""
                      headerGroupName: '{{column.HeaderGroupName?.Replace("'", "\\'")}}',
                    """)}}
                    }
                    """;
            }
        }
    }
}
