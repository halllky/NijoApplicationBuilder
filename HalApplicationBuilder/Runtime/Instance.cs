using System;
namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// SingleView, CreateView, 集約PartialView の編集対象オブジェクト
    /// </summary>
    public class Instance<TInstanceModel> {
        public TInstanceModel Item { get; set; }

        /// <summary>更新確定時に削除される要素か否か</summary>
        public bool Deleted { get; set; }
    }
}
