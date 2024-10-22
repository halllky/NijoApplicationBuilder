using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NIJO_APPLICATION_TEMPLATE {
    /// <summary>
    /// 添付ファイルまたは永続化された添付ファイルのメタデータ。
    /// </summary>
    public partial class FileAttachmentMetadata {
        /// <summary>
        /// 永続化された添付ファイルにアクセスするためのID
        /// </summary>
        [JsonPropertyName("fileAttachmentId")]
        public FileAttachmentId? FileAttachmentId { get; set; }
        /// <summary>
        /// 画面上に表示するファイル名
        /// </summary>
        [JsonPropertyName("displayFileName")]
        public string? DisplayFileName { get; set; }
        /// <summary>
        /// IDと名前以外の属性
        /// </summary>
        [JsonPropertyName("otherProps")]
        public Dictionary<string, string> OtherProps { get; set; } = [];
    }


    /// <summary>
    /// 添付ファイルの永続化を行います。
    /// </summary>
    public interface IFileAttachmentRepository {
        /// <summary>
        /// 添付ファイルのコンテンツを保存します。
        /// </summary>
        /// <param name="id">ファイルID</param>
        /// <param name="fileName">ファイル名</param>
        /// <param name="stream">ファイルコンテンツ</param>
        /// <param name="errors">エラーメッセージがある場合はここに追加する</param>
        Task SaveFileAsync(FileAttachmentId id, string fileName, Stream stream, ICollection<string> errors);
        /// <summary>
        /// 保存されているファイルをIDで探します。
        /// </summary>
        /// <param name="id">ファイルID</param>
        /// <returns>ファイルが見つからない場合はnullを返します。</returns>
        Stream? FindFile(FileAttachmentId id, out string? fileName);
    }


    /// <summary>
    /// 添付ファイルID。
    /// クライアント側からアップロードされたファイルは、通常のデータの登録更新のトランザクションとは別に
    /// サーバー上の特定のフォルダ内など任意の場所に保存されるが、
    /// アプリケーションサービスなどからその保存されたファイルにアクセスする場合はこのIDを用いて行う。
    /// </summary>
    public sealed class FileAttachmentId {
        public FileAttachmentId(string value) {
            _value = value;
        }
        private readonly string _value;

        #region 同一性比較用
        public override bool Equals(object? obj) {
            if (obj is not FileAttachmentId other) return false;
            if (other._value != _value) return false;
            return true;
        }
        public override int GetHashCode() {
            return _value.GetHashCode();
        }
        public override string ToString() {
            return _value;
        }

        public static bool operator ==(FileAttachmentId? left, FileAttachmentId? right) {
            if (left is null ^ right is null) return false;
            return ReferenceEquals(left, right) || left!.Equals(right);
        }
        public static bool operator !=(FileAttachmentId? left, FileAttachmentId? right) {
            return !(left == right);
        }
        #endregion 同一性比較用

        #region DBやJSONとの変換
        /// <summary>
        /// Entity Frameword Core 関連の処理で使用される、
        /// <see cref="FileAttachmentId"/> 型のプロパティと、DBのカラムの型変換。
        /// </summary>
        public class EfCoreValueConverter : ValueConverter<FileAttachmentId, string> {
            public EfCoreValueConverter() : base(
                csValue => csValue._value,
                dbValue => new FileAttachmentId(dbValue),
                new ConverterMappingHints(size: 255)) { }
        }
        /// <summary>
        /// HTTPリクエスト・レスポンスの処理で使用される、
        /// <see cref="FileAttachmentId"/> 型のプロパティと、JSONプロパティの型変換。
        /// </summary>
        public class JsonValueConverter : JsonConverter<FileAttachmentId> {
            public override FileAttachmentId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                var clrValue = reader.GetString();
                return string.IsNullOrWhiteSpace(clrValue)
                    ? null
                    : new FileAttachmentId(clrValue);
            }
            public override void Write(Utf8JsonWriter writer, FileAttachmentId? value, JsonSerializerOptions options) {
                if (value == null) {
                    writer.WriteNullValue();
                } else {
                    writer.WriteStringValue(value.ToString());
                }
            }
        }
        #endregion DBやJSONとの変換
    }
}
