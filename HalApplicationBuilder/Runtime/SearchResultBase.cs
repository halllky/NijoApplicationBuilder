using System;
using System.Diagnostics.CodeAnalysis;

namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// 検索結果
    /// </summary>
    public abstract class SearchResultBase {
        [SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "フレームワークで使用するプロパティ")]
        public string __halapp__InstanceKey { get; set; }
    }
}
