namespace NIJO_APPLICATION_TEMPLATE {
    /// <summary>
    /// 添付ファイル保存処理のWebアプリケーション側の実装。
    /// </summary>
    public class AttachmentFileRepositoryWeb : IFileAttachmentRepository {

        public AttachmentFileRepositoryWeb(RuntimeSettings.Server settings) {
            _settings = settings;
        }
        private readonly RuntimeSettings.Server _settings;


        public Stream? FindFile(FileAttachmentId id, out string? fileName) {
            var dirName = Path.Combine(GetStorageDirectory(), id.ToString());
            if (!Directory.Exists(dirName)) {
                fileName = null;
                return null;
            }

            var files = Directory.GetFiles(dirName);
            if (files.Length == 0) {
                fileName = null;
                return null;
            }
            if (files.Length >= 2) {
                throw new InvalidOperationException($"1つの添付ファイル保存ディレクトリ内に複数のファイルが存在します: {dirName}");
            }

            fileName = Path.GetFileName(files[0]);
            return new FileStream(files[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }


        public async Task SaveFileAsync(FileAttachmentId id, string fileName, Stream stream, ICollection<string> errors) {
            // 入力エラーチェック
            var invalidChar = _invalidChar ??= Path.GetInvalidFileNameChars();
            if (id.ToString().Any(c => invalidChar.Contains(c))) {
                errors.Add($"ファイルID '{id}' にファイル名として無効な文字が含まれています。");
            }
            if (fileName.Any(c => invalidChar.Contains(c))) {
                errors.Add($"ファイル '{fileName}' のファイル名に無効な文字が含まれています。");
            }
            if (errors.Count > 0) return;

            /// 格納先フォルダを作成
            var dirName = Path.Combine(GetStorageDirectory(), id.ToString());
            Directory.CreateDirectory(dirName);

            // 保存
            var path = Path.Combine(dirName, fileName);
            using var sw = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(sw);
        }


        /// <summary>
        /// 添付ファイル保存先ディレクトリを返します。
        /// </summary>
        private string GetStorageDirectory() {
            if (_cache == null) {
                // 添付ファイルが保存されるディレクトリを決定
                if (string.IsNullOrWhiteSpace(_settings.UploadedFileDir))
                    throw new InvalidOperationException("添付ファイル保存先ディレクトリが設定されていません。");

                _cache = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _settings.UploadedFileDir));
            }
            return _cache;
        }
        private string? _cache;


        // ファイル名に使用できない文字の一覧
        private static char[]? _invalidChar;
    }
}
