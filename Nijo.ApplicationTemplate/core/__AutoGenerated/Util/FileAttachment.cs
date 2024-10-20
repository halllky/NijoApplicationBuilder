namespace NIJO_APPLICATION_TEMPLATE {
    /// <summary>
    /// 添付ファイルまたは永続化された添付ファイルのメタデータ。
    /// 「ファイルがアップロードされようとしている状態（このインスタンスにファイルコンテンツがある）」、
    /// 「ファイルは既にどこかに永続化されており、このインスタンスはそのファイルの名前や保存場所などメタデータのみを持つ状態」、
    /// といった複数の状態を持つ。
    /// </summary>
    public partial class FileAttachment {

        /// <summary>
        /// アップロードされようとしているファイル。
        /// このプロパティがnullでない状態で、このクラスのインスタンスを項目に持つWriteModelがDB保存されようとした場合、
        /// 例外が発生します（ファイル保存処理の実装漏れ回避のため）。
        /// 永続化処理完了後、このプロパティにnullを代入してください。
        /// </summary>
        public Func<Stream>? GetUploadingFile { get; set; }

        /// <summary>
        /// 永続化されたファイルのメタデータ
        /// </summary>
        public FileAttachmentMetadata? Metadata { get; set; }
        /// <summary>
        /// クライアント側でこのファイルを削除するよう指定された場合はtrue
        /// </summary>
        public bool WillDetach { get; set; }
    }

    /// <summary>
    /// 永続化されたファイルのメタデータ
    /// </summary>
    public partial class FileAttachmentMetadata {
        /// <summary>
        /// 画面上に表示するファイル名
        /// </summary>
        public string? DisplayFileName { get; set; }
        /// <summary>
        /// 画面上で表示したときのaタグのリンク
        /// </summary>
        public string? Href { get; set; }
        /// <summary>
        /// 画面上で表示したときのaタグのクリック時にブラウザのダウンロード開始を行うかどうか
        /// </summary>
        public bool Download { get; set; }
        /// <summary>
        /// 上記以外の属性
        /// </summary>
        public Dictionary<string, string> OtherProps { get; set; } = [];
    }
}
