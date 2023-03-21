using System;
using System.Diagnostics.CodeAnalysis;

namespace HalApplicationBuilder.Runtime {

    /// <summary>
    /// SingleView, CreateView, 集約PartialView の編集対象オブジェクト
    /// </summary>
    public abstract class UIInstanceBase {
        /// <summary>
        /// 当初 __halapp__ という名前にしていたが、アンダースコアの関係なのか、name属性が正しくレンダリングされなかったためこの名前にしている
        /// </summary>
        [SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "フレームワークで使用するプロパティ")]
        public HalappViewState halapp_fields { get; set; } = new();
    }

    public sealed class HalappViewState {
        /// <summary>集約ルートか否か。ルートなら右肩の削除ボタンが表示されない、などに使用</summary>
        public bool IsRoot { get; set; }
        /// <summary>更新確定時に削除される要素か否か</summary>
        public bool Removed { get; set; }
    }
}
