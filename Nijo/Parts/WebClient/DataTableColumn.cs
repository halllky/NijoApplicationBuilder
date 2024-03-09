using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class DataTableColumn {

        internal static DataTableColumn FromMember(
            AggregateMember.AggregateMemberBase member,
            string rowAccessor,
            GraphNode<Aggregate> dataTableOwner,
            string colId,
            bool readOnly) {

            var memberPath = member is AggregateMember.ValueMember vm
                ? vm.Declared.GetFullPath(since: dataTableOwner)
                : member.GetFullPath(since: dataTableOwner);

            var cell = $$"""
                cellProps => {
                  const value = cellProps.row.original.{{rowAccessor}}.{{memberPath.Join("?.")}}
                  const formatted = typeof value === 'object'
                    ? JSON.stringify(value)
                    : (value as React.ReactNode)
                  return (
                    <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                      {formatted}
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
                cellEditor = readOnly
                    ? null
                    : $"(props, ref) => <Input.{combobox.ComponentName} ref={{ref}} {{...props}} />";

            } else {
                throw new InvalidProgramException();
            }

            var getValue = $"data => data.{rowAccessor}.{memberPath.Join(".")}";

            string? setValue;
            if (readOnly) {
                setValue = null;
            } else if (member.DeclaringAggregate == dataTableOwner) {
                setValue = $"(row, value) => row.{rowAccessor}.{memberPath.Join(".")} = value";
            } else {
                setValue = $$"""
                    (row, value) => {
                      if (data.{{rowAccessor}}.{{memberPath.SkipLast(1).Join("?.")}})
                        data.{{rowAccessor}}.{{memberPath.Join(".")}} = value
                    }
                    """;
            }

            return new DataTableColumn {
                Id = colId,
                Header = member.MemberName,
                Cell = cell,
                CellEditor = cellEditor,
                GetValue = getValue,
                SetValue = setValue,
            };
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
                },
                """;
        }
    }
}
