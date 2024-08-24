using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient.DataTable {
    /// <summary>
    /// Reactテンプレート側で宣言されているコンポーネント DataTable の列定義。
    /// <see cref="Models.ReadModel2"/> 用。
    /// 将来的に <see cref="DataTableColumn"/> を廃止してこちらに統合する予定。
    /// </summary>
    internal interface IDataTableColumn2 {
        // 基本
        /// <summary>列ヘッダ文字</summary>
        string Header { get; }
        /// <summary>列グルーピング</summary>
        string? HeaderGroupName { get; }

        // 外観
        bool Hidden { get; }
        int? DefaultWidth { get; }
        bool EnableResizing { get; }

        /// <summary>
        /// 非編集時のセルの表示内容をレンダリングします。
        /// </summary>
        string RenderDisplayContents(CodeRenderingContext ctx);

        /// <summary>編集設定。nullの場合は常に編集不可の列であることを意味する</summary>
        IGridColumnSetting? GetEditSetting();
        /// <summary>セル編集開始時の値取得処理をレンダリングします。</summary>
        string RenderGetterOnEditStart(CodeRenderingContext ctx);
        /// <summary>セル編集終了時の値設定処理をレンダリングします。</summary>
        string RenderSetterOnEditEnd(CodeRenderingContext ctx);
        /// <summary>セルの値をクリップボードへコピーする処理をレンダリングします。</summary>
        string RenderOnClipboardCopy(CodeRenderingContext ctx);
        /// <summary>クリップボードの内容をセルへペーストする処理をレンダリングします。</summary>
        string RenderOnClipboardPaste(CodeRenderingContext ctx);
    }
}
