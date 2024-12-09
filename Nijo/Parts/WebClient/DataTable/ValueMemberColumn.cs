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

        internal readonly AggregateMember.ValueMember _vm;
        internal readonly IEnumerable<string> _pathFromRowObject;
        private readonly DataTableBuilder _tableContext;

        public string Header => _vm.DisplayName;
        public string? HeaderGroupName => _vm.Owner == _tableContext.TableOwner ? null : _vm.Owner.Item.DisplayName;

        public int? DefaultWidth => null;
        public bool EnableResizing => true;
    }
}
