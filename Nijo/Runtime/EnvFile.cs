using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nijo.Runtime {
    /// <summary>
    /// .env ファイル。Viteによって読み込まれ、
    /// Reactのビルドの過程でビルド後のソースに組み込まれる。
    /// </summary>
    internal class EnvFile {

        internal EnvFile(Uri webapiServerUrl, string reactProjectRoot, ILogger logger) {
            _webapiServerUrl = webapiServerUrl;
            _reactProjectRoot = reactProjectRoot;
            _logger = logger;
        }
        private readonly Uri _webapiServerUrl;
        private readonly string _reactProjectRoot;
        private readonly ILogger _logger;

        internal void Overwrite() {
            try {
                // この名前の環境ファイルはdevelopmentモードでのみ読み込まれる。またgitには無視される
                const string FILENAME = ".env.development.local";

                var filepath = Path.Combine(_reactProjectRoot, FILENAME);
                var fileEncoding = new UTF8Encoding(false, false);
                const string NEWLINE = "\n";

                var url = _webapiServerUrl;

                if (!File.Exists(filepath)) {
                    var value = $"VITE_BACKEND_API={url}{NEWLINE}";
                    File.WriteAllText(filepath, value, fileEncoding);

                } else {
                    var text = File.ReadAllText(filepath, fileEncoding);
                    var regex = new Regex(@"^VITE_BACKEND_API=(.*)$", RegexOptions.Multiline);
                    if (regex.IsMatch(text)) {
                        var replaced = regex.Replace(text, $"VITE_BACKEND_API={url}");
                        File.WriteAllText(filepath, replaced);

                    } else {
                        var appended = text.EndsWith(NEWLINE)
                            ? $"{text}VITE_BACKEND_API={url}{NEWLINE}"
                            : $"{text}{NEWLINE}VITE_BACKEND_API={url}{NEWLINE}";
                        File.WriteAllText(filepath, appended);
                    }
                }
            } catch (Exception ex) {
                _logger.LogWarning(".envファイルの更新に失敗しました: {err}", ex.Message);
            }
        }
    }
}
