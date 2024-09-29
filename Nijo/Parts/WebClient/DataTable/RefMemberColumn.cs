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

        internal readonly AggregateMember.Ref _ref;
        internal readonly IEnumerable<string> _pathFromRowObject;
        private readonly DataTableBuilder _tableContext;

        public string Header => _ref.DisplayName;
        public string? HeaderGroupName => _ref.Owner == _tableContext.TableOwner ? null : _ref.Owner.Item.DisplayName;

        public int? DefaultWidth => null;
        public bool EnableResizing => true;
    }
}
