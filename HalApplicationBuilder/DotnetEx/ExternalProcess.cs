using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal class ExternalProcess {
        internal ExternalProcess(string workingDirectory, CancellationToken? cancellationToken = null) {
            _workingDirectory = workingDirectory;
            _cancellationToken = cancellationToken;
        }

        private readonly string _workingDirectory;
        private readonly CancellationToken? _cancellationToken;

        internal void Start(string filename, params string[] args) {
            using var cmd = new System.Diagnostics.Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // windowsでnvmを使うとき直にnpmを実行できないためcmd経由で実行する
                cmd.StartInfo.FileName = "cmd";
                cmd.StartInfo.ArgumentList.Add("/c");
                cmd.StartInfo.ArgumentList.Add($"{filename} {string.Join(" ", args)}");
            } else {
                cmd.StartInfo.FileName = filename;
                foreach (var arg in args) cmd.StartInfo.ArgumentList.Add(arg);
            }
            cmd.StartInfo.WorkingDirectory = _workingDirectory;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
            cmd.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
            cmd.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            cmd.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            try {
                _cancellationToken?.ThrowIfCancellationRequested();

                cmd.Start();
                cmd.BeginOutputReadLine();
                cmd.BeginErrorReadLine();
                while (!cmd.HasExited) {
                    _cancellationToken?.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
                if (cmd.ExitCode != 0) {
                    throw new InvalidOperationException($"外部コマンド( {filename} {string.Join(" ", args)})実行時にエラーが発生しました。");
                }

            } catch (OperationCanceledException) {
                cmd.Kill(entireProcessTree: true);

                // Killの完了前に新規プロジェクト作成時の一時ディレクトリの削除処理が走ってしまい
                // 削除できなくて失敗することがあるので少し待つ
                Thread.Sleep(1000);

                throw;

            } catch (Exception ex) {
                throw new Exception($"EXCEPTION: '{filename} {string.Join(" ", args)}'", ex);
            }
        }
    }
}
