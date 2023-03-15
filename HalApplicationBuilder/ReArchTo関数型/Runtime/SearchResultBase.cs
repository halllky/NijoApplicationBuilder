using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

namespace HalApplicationBuilder.ReArchTo関数型.Runtime {
    /// <summary>
    /// 検索結果
    /// </summary>
    public abstract class SearchResultBase {
        [SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "フレームワークで使用するプロパティ")]
        public string __halapp__InstanceKey { get; set; }
    }
}
