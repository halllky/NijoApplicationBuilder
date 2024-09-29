using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient.DataTable {
    /// <summary>
    /// 特定の集約メンバーに紐づかない列定義。例えばボタンの列やリンクの列など
    /// </summary>
    internal class AdhocColumn : IDataTableColumn2 {
        public string Header { get; init; } = string.Empty;
        public string? HeaderGroupName { get; init; }
        public int? DefaultWidth { get; init; }
        public bool EnableResizing { get; init; }
        public required Func<CodeRenderingContext, string, string, string> CellContents { get; init; }
    }
}
