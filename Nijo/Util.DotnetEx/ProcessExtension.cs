using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx;

public static class ProcessExtension {

    /// <summary>
    /// 標準出力か標準エラー出力かの別
    /// </summary>
    public enum E_STD {
        StdOut,
        StdErr,
    }

    /// <summary>
    /// Process.Startのラッパーメソッド
    /// </summary>
    /// <param name="editStartInfo">StartInfoをカスタマイズする</param>
    /// <param name="logOut">標準出力 or 標準エラー出力</param>
    /// <param name="timeout">タイムアウト。既定は30秒</param>
    /// <returns>EXIT CODE</returns>
    public static async Task<int> ExecuteProcessAsync(Action<ProcessStartInfo> editStartInfo, Action<E_STD, string> logOut, TimeSpan? timeout = null) {
        using var process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
        process.StartInfo.RedirectStandardError = true;  // 標準エラーをリダイレクト
        editStartInfo(process.StartInfo);

        process.OutputDataReceived += (sender, e) => {
            try {
                if (e.Data != null) {
                    logOut(E_STD.StdOut, e.Data);
                } else {
                    logOut(E_STD.StdOut, $"OutputDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、無視する
                logOut(E_STD.StdOut, $"Caught InvalidOperationException in OutputDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                logOut(E_STD.StdOut, $"EXCEPTION in OutputDataReceived: {ex.ToString()}");
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            try {
                if (e.Data != null) {
                    logOut(E_STD.StdErr, e.Data);
                } else {
                    logOut(E_STD.StdErr, $"ErrorDataReceived: Stream closed (Data is null).");
                }
            } catch (InvalidOperationException ioex) {
                // プロセス終了時などにストリームが閉じられてこの例外が発生することがあるため、ログのみ出力して無視する
                logOut(E_STD.StdErr, $"Caught InvalidOperationException in ErrorDataReceived (likely harmless): {ioex.Message}");
            } catch (Exception ex) {
                logOut(E_STD.StdErr, $"EXCEPTION in ErrorDataReceived: {ex.ToString()}");
            }
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var timeoutLimit = DateTime.Now.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (true) {
            if (DateTime.Now > timeoutLimit) {
                EnsureKill(process);
                process.CancelOutputRead();
                process.CancelErrorRead();
                throw new TimeoutException();

            } else if (process.HasExited) {
                process.CancelOutputRead();
                process.CancelErrorRead();
                return process.ExitCode;

            } else {
                await Task.Delay(100);
            }
        }
    }

    /// <summary>
    /// プロセスツリーを確実に終了させます。
    /// </summary>
    /// <returns>処理結果</returns>
    public static string EnsureKill(this Process process) {
        int? pid = null;
        try {
            if (process.HasExited) return "Process is already exited. kill is skipped.";

            pid = process.Id;

            // npm run dev → vite、dotnet watch → アプリ本体 のような親子構成のプロセスを
            // 子も含めて確実に終了させる。親だけをkillすると孤児になった子がポートを
            // 掴んだままになり、次回起動が strictPort 等で失敗する。
            process.Kill(entireProcessTree: true);

            // ポートが解放される前に次のプロセスを起動してしまわないよう、終了を待つ
            if (!process.WaitForExit(TimeSpan.FromSeconds(5))) {
                return $"Kill signal sent but the process did not exit within 5 seconds (PID = {pid})";
            }

            return $"Success to kill process tree (PID = {pid})";

        } catch (Exception ex) {
            return $"Failed to kill process tree (PID = {pid}): {ex.Message}";
        }
    }

    /// <summary>
    /// .cmd ファイルを、文字コードや行末処理を加えたうえで出力する。
    /// 特にdotnetコマンド（暗黙的にutf8で実行される）を呼び出す状況を考慮している。
    /// </summary>
    /// <param name="cmdFilePath">ファイルパス</param>
    /// <param name="cmdFileContent">ファイルの内容</param>
    /// <param name="fileEncoding">エンコード。既定ではBOMなしUTF8</param>
    public static void RenderCmdFile(string cmdFilePath, string cmdFileContent, Encoding? fileEncoding = null) {
        File.WriteAllText(
            cmdFilePath,
            // cmd処理中にchcpしたときは各行の改行コードの前にスペースが無いと上手く動かないので
            cmdFileContent.ReplaceLineEndings(" \r\n"),
            fileEncoding ?? new UTF8Encoding(false, false));
    }
}
