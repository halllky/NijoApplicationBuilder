using System;
using System.Diagnostics.CodeAnalysis;

namespace HalApplicationBuilder.Core.UIModel {

    /// <summary>
    /// SingleView, CreateView, 集約PartialView の編集対象オブジェクト
    /// </summary>
    public abstract class UIInstanceBase {
        [SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "フレームワークで使用するプロパティ")]
        public HalappViewState __halapp__ { get; set; } = new();
    }

    public sealed class HalappViewState {
        /// <summary>集約ルートか否か。ルートなら右肩の削除ボタンが表示されない、などに使用</summary>
        public bool IsRoot { get; set; }
        /// <summary>更新確定時に削除される要素か否か</summary>
        public bool Removed { get; set; }
    }
}
