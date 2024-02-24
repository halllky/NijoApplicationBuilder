using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Runtime {
    internal class GeneratedProjectBuilder {
        internal GeneratedProjectBuilder(GeneratedProject project, ILogger logger) {
            _project = project;
            _logger = logger;
        }

        private readonly GeneratedProject _project;
        private readonly ILogger _logger;

        /// <summary>
        /// 静的検査。結果は戻り値ではなくログに出力される。
        /// </summary>
        internal async Task<bool> StaticCheck(CancellationToken cancellationToken) {
            var npmRun = new Process();
            var dotnetRun = new Process();
            try {
                npmRun.StartInfo.WorkingDirectory = _project.WebClientProjectRoot;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    npmRun.StartInfo.FileName = "powershell";
                    npmRun.StartInfo.Arguments = "/c \"npm run tsc\"";
                } else {
                    npmRun.StartInfo.FileName = "npm";
                    npmRun.StartInfo.Arguments = "run tsc";
                }
                npmRun.StartInfo.RedirectStandardOutput = true;
                npmRun.StartInfo.RedirectStandardError = true;
                npmRun.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                npmRun.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                npmRun.OutputDataReceived += OnNpmStdOut;
                npmRun.ErrorDataReceived += OnNpmStdErr;

                npmRun.Start();
                npmRun.BeginOutputReadLine();
                npmRun.BeginErrorReadLine();
                _logger.LogInformation("npm build   : Started. PID {PID}", npmRun.Id);

                dotnetRun.StartInfo.WorkingDirectory = _project.WebApiProjectRoot;
                dotnetRun.StartInfo.FileName = "dotnet";
                dotnetRun.StartInfo.Arguments = "build";
                dotnetRun.StartInfo.RedirectStandardOutput = true;
                dotnetRun.StartInfo.RedirectStandardError = true;
                dotnetRun.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
                dotnetRun.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
                dotnetRun.OutputDataReceived += OnDotnetStdOut;
                dotnetRun.ErrorDataReceived += OnDotnetStdErr;

                dotnetRun.Start();
                dotnetRun.BeginOutputReadLine();
                dotnetRun.BeginErrorReadLine();
                _logger.LogInformation("dotnet build: Started. PID {PID}", dotnetRun.Id);

                await Task.WhenAll(
                    dotnetRun.WaitForExitAsync(cancellationToken),
                    npmRun.WaitForExitAsync(cancellationToken));

                return dotnetRun.ExitCode == 0 && npmRun.ExitCode == 0;

            } finally {
                _logger.LogInformation("npm build   : {msg}", npmRun.EnsureKill());
                _logger.LogInformation("dotnet build: {msg}", dotnetRun.EnsureKill());
            }
        }

        private void OnDotnetStdOut(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) _logger.LogTrace("dotnet build: {Data}", e.Data);
        }
        private void OnDotnetStdErr(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) _logger.LogError("dotnet build: {Data}", e.Data);
        }

        private void OnNpmStdOut(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) _logger.LogTrace("npm build   : {Data}", e.Data);
        }
        private void OnNpmStdErr(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) _logger.LogError("npm build   : {Data}", e.Data);
        }
    }
}
