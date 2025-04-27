using System.Text;

namespace Nijo.Mcp;

partial class NijoMcpTools {
    /// <summary>
    /// `nijo run` したときにキャンセル用のファイルを指定できるので、
    /// そこにファイルを出力し、完了まで一定時間待つ。
    /// </summary>
    public static async Task 既存デバッグプロセス中止(WorkDirectory workDirectory) {
        try {
            using var writer = new StreamWriter(workDirectory.NijoExeCancelFile, append: true, encoding: new UTF8Encoding(false, false));
            writer.WriteLine(""); // ファイルがあればよいので中身は空

            while (true) {
                var status = await デバッグプロセス稼働判定(workDirectory, TimeSpan.FromSeconds(2));

                // 終了
                if (!status.DotnetIsReady && !status.NpmIsReady) break;

                await Task.Delay(500);
            }

        } finally {
            try {
                if (File.Exists(workDirectory.NijoExeCancelFile)) {
                    File.Delete(workDirectory.NijoExeCancelFile);
                }
            } catch {
                // 何もしない
            }
        }

    }
}
