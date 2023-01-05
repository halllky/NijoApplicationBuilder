using System;
namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// SingleView, CreateView, 集約PartialView の編集対象オブジェクト
    /// </summary>
    public class Instance<TInstanceModel> {
        public TInstanceModel Item { get; set; }

        /// <summary>集約ルートか否か。ルートなら右肩の削除ボタンが表示されない、などに使用</summary>
        public bool IsRoot { get; set; }
        /// <summary>更新確定時に削除される要素か否か</summary>
        public bool Removed { get; set; }
    }
}
