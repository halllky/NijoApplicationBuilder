using System.Text.Json.Serialization;

namespace NIJO_APPLICATION_TEMPLATE_WebApi {
    /// <summary>
    /// 添付ファイルまたは永続化された添付ファイルのメタデータ
    /// </summary>
    public partial class FileAttachment {
        /// <summary>
        /// ファイルがアップロードされようとしている場合はここに値が入る
        /// </summary>
        [JsonPropertyName("file")]
        public IFormFile? File { get; set; }
        /// <summary>
        /// 永続化されたファイルのメタデータ
        /// </summary>
        [JsonPropertyName("metadata")]
        public FileAttachmentMetadata? Metadata { get; set; }
        /// <summary>
        /// 永続化されたファイルを削除しようとしている場合はtrue
        /// </summary>
        [JsonPropertyName("willDetach")]
        public bool WillDetach { get; set; }
    }

    /// <summary>
    /// 永続化されたファイルのメタデータ
    /// </summary>
    public partial class FileAttachmentMetadata {
        /// <summary>
        /// 画面上に表示するファイル名
        /// </summary>
        [JsonPropertyName("displayFileName")]
        public string? DisplayFileName { get; set; }
        /// <summary>
        /// 画面上で表示したときのaタグのリンク
        /// </summary>
        [JsonPropertyName("href")]
        public string? Href { get; set; }
        /// <summary>
        /// 画面上で表示したときのaタグのクリック時にブラウザのダウンロード開始を行うかどうか
        /// </summary>
        [JsonPropertyName("download")]
        public bool Download { get; set; }
    }
}
