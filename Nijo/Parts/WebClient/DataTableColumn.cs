using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class DataTableColumn {

        internal static IEnumerable<DataTableColumn> FromMembers(
            string rowAccessor,
            GraphNode<Aggregate> dataTableOwner,
            bool readOnly) {

            var colIndex = 0;

            // AggregateMemberを列定義に変換する関数
            DataTableColumn ToDataTableColumn(AggregateMember.AggregateMemberBase member) {
                var vm = member as AggregateMember.ValueMember;
                var refMember = member as AggregateMember.Ref;

                var memberPath = member.GetFullPathAsSingleViewDataClass();
                var ownerPath = member.Owner.GetFullPathAsSingleViewDataClass();

                // 非編集時のセル表示文字列
                string? formatted = null;
                if (vm != null) {
                    var component = vm.Options.MemberType.GetReactComponent(new() {
                        Type = GetReactComponentArgs.E_Type.InDataGrid,
                    });
                    if (component.GridCellFormatStatement != null) {
                        formatted = component.GridCellFormatStatement("value", "formatted");
                    }
                } else if (refMember != null) {
                    var names = refMember.MemberAggregate
                        .AsEntry() // 以下のような場合にエラーになるのでエントリー化する:
                                   // 参照先集約のSingleViewのChildrenのDataTableで、参照元を一緒に表示、
                                   // かつ参照元の表示名称に参照先がふくまれている場合
                                   // （有向グラフの経路で言うと参照先→参照元→参照先のようにぐるっと回って戻ってくるパターン）
                        .GetNames()
                        .OfType<AggregateMember.ValueMember>()
                        .Select(name => name.Declared.GetFullPath().ToList());
                    formatted = $$"""
                        let formatted = ''
                        {{names.SelectTextTemplate(name => $$"""
                        if (value?.{{name.Join("?.")}} != null) formatted += String(value.{{name.Join(".")}})
                        """)}}
                        """;
                }

                var cell = $$"""
                    cellProps => {
                      const value = cellProps.row.original.{{rowAccessor}}.{{memberPath.Join("?.")}}
                      {{If(formatted != null, () => WithIndent(formatted!, "  "))}}
                      return (
                        <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                          {{{(formatted == null ? "value" : "formatted")}}}
                          &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
                        </span>
                      )
                    }
                    """;

                string? cellEditor;
                if (readOnly) {
                    cellEditor = null;
                } else if (member is AggregateMember.ValueMember vm2) {
                    var editor = vm2.Options.MemberType.GetReactComponent(new() {
                        Type = GetReactComponentArgs.E_Type.InDataGrid,
                    });
                    cellEditor = $"(props, ref) => <{editor.Name} ref={{ref}} {{...props}}{string.Concat(editor.GetPropsStatement())} />";

                } else if (member is AggregateMember.Ref rm2) {
                    var combobox = new ComboBox(rm2.MemberAggregate);
                    cellEditor = $"(props, ref) => <Input.{combobox.ComponentName} ref={{ref}} {{...props}} />";

                } else {
                    throw new InvalidProgramException();
                }

                var getValue = $"data => data.{rowAccessor}.{memberPath.Join("?.")}";

                string? setValue;
                if (readOnly) {
                    setValue = null;
                } else if (member.DeclaringAggregate == dataTableOwner) {
                    setValue = $"(row, value) => row.{rowAccessor}.{memberPath.Join(".")} = value";
                } else {
                    setValue = $$"""
                        (row, value) => {
                          if (row.{{rowAccessor}}.{{ownerPath.Join("?.")}}) {
                            row.{{rowAccessor}}.{{memberPath.Join(".")}} = value
                            row.{{rowAccessor}}.{{ownerPath.Join(".")}}.{{DisplayDataClass.WILL_BE_CHANGED}} = true
                          }
                        }
                        """;
                }

                var hidden = vm?.Options.InvisibleInGui == true
                    ? true
                    : (bool?)null;

                var headerGroupName = member.Owner == dataTableOwner
                    ? null
                    : member.Owner.Item.DisplayName;

                colIndex++;

                return new DataTableColumn {
                    Id = $"col{colIndex}",
                    Header = member.MemberName,
                    Cell = cell,
                    CellEditor = cellEditor,
                    GetValue = getValue,
                    SetValue = setValue,
                    Hidden = hidden,
                    HeaderGroupName = headerGroupName,
                };
            }
            // ----------------------------------------------------

            // グリッドに表示するメンバーを列挙
            IEnumerable<DataTableColumn> Collect(DisplayDataClass dataClass) {
                foreach (var prop in dataClass.GetOwnProps()) {
                    if (prop.Member is AggregateMember.ValueMember vm) {
                        if (vm.DeclaringAggregate != dataClass.MainAggregate) continue;
                        if (vm.Options.InvisibleInGui) continue;
                        yield return ToDataTableColumn(prop.Member);

                    } else if (prop.Member is AggregateMember.Ref @ref) {
                        if (@ref.MemberAggregate.IsSingleRefKeyOf(@ref.Owner)) continue;
                        yield return ToDataTableColumn(prop.Member);
                    }
                }

                foreach (var prop in dataClass.GetChildProps()) {

                    // ChildrenやVariationのメンバーを列挙していないのはグリッド上で表現できないため
                    if (prop.MemberInfo is AggregateMember.Children) continue;
                    if (prop.MemberInfo is AggregateMember.VariationItem) continue;

                    foreach (var reucusive in Collect(new DisplayDataClass(prop.MainAggregate))) {
                        yield return reucusive;
                    }
                }
            }

            // ソース中にラムダ式が登場するのでエントリー化
            var root = new DisplayDataClass(dataTableOwner.AsEntry());

            foreach (var column in Collect(root)) {
                yield return column;
            }
        }

        // react table のAPI
        internal required string Id { get; init; }
        internal required string Header { get; init; }
        internal required string Cell { get; init; }
        internal int? Size { get; init; }
        internal bool? EnableResizing { get; init; }
        /// <summary>accessorFnにマッピングされる</summary>
        internal string? GetValue { get; init; }

        // 独自定義
        internal string? CellEditor { get; init; }
        internal string? SetValue { get; init; }
        internal bool? Hidden { get; init; }
        internal string? HeaderGroupName { get; init; }

        internal string Render() {
            return $$"""
                {
                  id: '{{Id}}',
                  header: '{{Header}}',
                  cell: {{WithIndent(Cell, "  ")}},
                {{If(Size != null, () => $$"""
                  size: {{Size}},
                """)}}
                {{If(EnableResizing != null, () => $$"""
                  enableResizing: {{(EnableResizing!.Value ? "true" : "false")}},
                """)}}
                {{If(GetValue != null, () => $$"""
                  accessorFn: {{GetValue}},
                """)}}
                {{If(SetValue != null, () => $$"""
                  setValue: {{WithIndent(SetValue!, "  ")}},
                """)}}
                {{If(CellEditor != null, () => $$"""
                  cellEditor: {{WithIndent(CellEditor!, "  ")}},
                """)}}
                {{If(Hidden != null, () => $$"""
                  hidden: {{(Hidden!.Value ? "true" : "false")}},
                """)}}
                {{If(HeaderGroupName != null, () => $$"""
                  headerGroupName: '{{HeaderGroupName}}',
                """)}}
                },
                """;
        }
    }
}
