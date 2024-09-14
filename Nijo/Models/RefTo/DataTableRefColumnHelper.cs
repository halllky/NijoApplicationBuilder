using Nijo.Core;
using Nijo.Models.ReadModel2Features;
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

        internal string Render() {
            var displayData = new RefDisplayData(_refEntry, _refEntry);
            var keys = _refEntry
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => !vm.Options.InvisibleInGui);
            var names = _refEntry
                .GetNames()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => !vm.Options.InvisibleInGui);
            var refColumns = keys.Concat(names).ToArray();

            return $$"""
                /** {{_refEntry.Item.DisplayName}}を参照する列 */
                {{MethodName}}: {{Parts.WebClient.DataTable.CellType.HELPER_MEHOTD_TYPE}}<TRow, AggregateType.{{displayData.TsTypeName}} | undefined> = (header, getValue, setValue, opt) => {
                {{If(refColumns.Length > 0, () => $$"""
                  this
                """)}}
                {{refColumns.SelectTextTemplate(vm => $$"""
                    .{{vm.Options.MemberType.DataTableColumnDefHelperName}}('{{vm.MemberName}}',
                      row => getValue(row)?.{{vm.GetFullPathAsDataClassForRefTarget(since: _refEntry).Join("?.")}},
                      (r, v) => { /* TODO: 変更されたキーで再検索をかける */ }, {
                      ...opt,
                      headerGroupName: header,
                {{If(!vm.IsKey, () => $$"""
                      readOnly: true,
                """)}}
                    })
                """)}}
                  return this
                }
                """;
        }
    }
}
