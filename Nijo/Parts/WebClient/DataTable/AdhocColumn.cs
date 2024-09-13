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

        string IDataTableColumn2.RenderDisplayContents(CodeRenderingContext ctx, string arg, string argRowObject) {
            return CellContents(ctx, arg, argRowObject);
        }

        IGridColumnSetting? IDataTableColumn2.GetEditSetting() {
            return null;
        }
        string IDataTableColumn2.RenderGetterOnEditStart(CodeRenderingContext ctx) {
            throw new NotImplementedException(); // 編集不可のセルでこのメソッドが呼ばれることはない
        }
        string IDataTableColumn2.RenderOnClipboardCopy(CodeRenderingContext ctx) {
            return $$"""
                () => ''
                """;
        }
        string IDataTableColumn2.RenderOnClipboardPaste(CodeRenderingContext ctx) {
            throw new NotImplementedException(); // 編集不可のセルでこのメソッドが呼ばれることはない
        }
        string IDataTableColumn2.RenderSetterOnEditEnd(CodeRenderingContext ctx) {
            throw new NotImplementedException(); // 編集不可のセルでこのメソッドが呼ばれることはない
        }
    }
}
