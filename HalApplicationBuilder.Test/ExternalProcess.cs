using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Test {
    public class ExternalProcess : IDisposable {
        public string? WorkingDirectory { get; init; }

        private Process? _process = null;

        public Process? Process => _process;
        private readonly StringBuilder standardOutput = new();
        private readonly StringBuilder standardError = new();

        public string StartSync(string filename, params string[] args) {
            Start(filename, args);
            return WaitForEiit();
        }
        public string WaitForEiit() {
            if (_process == null) throw new InvalidOperationException("Process is not running.");
            _process.WaitForExit();
            return EnsureFinishProcess();
        }
        public void Start(string filename, params string[] args) {
            if (_process != null) throw new InvalidOperationException("Process is already running.");

            _process = new Process();
            _process.StartInfo.WorkingDirectory = WorkingDirectory;
            _process.StartInfo.FileName = filename;
            foreach (var arg in args) _process.StartInfo.ArgumentList.Add(arg);

            _process.StartInfo.UseShellExecute = false;

            standardOutput.Clear();
            standardError.Clear();
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            _process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            _process.OutputDataReceived += (sender, e) => {
                Console.WriteLine($"    <<<{filename} {string.Join(" ", args)}>>> :: {e.Data}");
                standardOutput.AppendLine(e.Data);
                OnStandardOutputReceived?.Invoke(this, e.Data);
            };
            _process.ErrorDataReceived += OnReceiveStandardError;
            _process.ErrorDataReceived += (sender, e) => {
                standardError.AppendLine(e.Data);
                OnStandardErrorReceived?.Invoke(this, e.Data);
            };

            Console.WriteLine($"PROCESS START: {filename} {string.Join(" ", args)}");

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public string? FindStandardOut(System.Text.RegularExpressions.Regex regex, int timeoutSeconds = 10) {
            if (_process == null) throw new InvalidOperationException("Process is not running.");
            var timeout = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= timeout) {
                var str = standardOutput.ToString().Split(Environment.NewLine);
                foreach (var line in str) {
                    var match = regex.Match(line);
                    if (match.Success) return match.Groups[1].Value;
                }
                Thread.Sleep(10);
            }
            return null;
        }

        public event EventHandler<string?>? OnStandardOutputReceived;
        public event EventHandler<string?>? OnStandardErrorReceived;

        private string EnsureFinishProcess() {
            if (_process == null) throw new InvalidOperationException("Process is not running.");

            Console.WriteLine($"PROCESS END:   {_process.StartInfo.FileName} {_process.StartInfo.Arguments}");

            var errors = standardError.ToString();
            if (_process.ExitCode != 0 || !string.IsNullOrWhiteSpace(errors)) {
                throw new InvalidOperationException(
                    $"プロセス\"{_process.StartInfo.FileName}\"の実行時にエラーが発生しました。" + Environment.NewLine +
                    $"実行時引数: " + _process.StartInfo.Arguments + Environment.NewLine +
                    $"終了コード: " + _process.ExitCode.ToString() + Environment.NewLine +
                    $"========== standard output =========" + Environment.NewLine +
                    standardOutput.ToString() + Environment.NewLine +
                    $"========== standard error ==========" + Environment.NewLine +
                    errors + Environment.NewLine +
                    $"====================================" + Environment.NewLine);
            }

            _process.Dispose();
            _process = null;

            return standardOutput.ToString();
        }

        /// <summary>
        /// 標準出力にエラーを検知したら結果を待たなくても例外を送出するための仕組み
        /// </summary>
        private void OnReceiveStandardError(object sender, DataReceivedEventArgs e) {
            if (_process == null) return;
            if (string.IsNullOrWhiteSpace(e.Data)) return;

            Thread.Sleep(new TimeSpan(0, 0, 5)); // とりあえずで5秒

            EnsureFinishProcess();
        }

        public void Dispose() {
            if (_process != null) {
                _process.Dispose();
                _process = null;
            }
        }
    }
}
