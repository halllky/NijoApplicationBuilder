using System.Diagnostics;
using System.Management;

namespace McpServer.Services {
    public class ProcessManager : IProcessManager, IDisposable {
        private readonly ILogger<ProcessManager> _logger;
        private readonly string _rootPath;
        private readonly string _reactPath;
        private readonly string _webApiPath;

        private Process? _reactProcess;
        private Process? _webApiProcess;

        private ProcessStatus _status = new();

        public ProcessManager(ILogger<ProcessManager> logger, IWebHostEnvironment environment) {
            Console.WriteLine($"ContentRootPath: {environment.ContentRootPath}");

            _logger = logger;
            _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, ".."));
            _reactPath = Path.Combine(_rootPath, "react");
            _webApiPath = Path.Combine(_rootPath, "WebApi");

            _logger.LogInformation($"Root path: {_rootPath}");
            _logger.LogInformation($"React path: {_reactPath}");
            _logger.LogInformation($"WebApi path: {_webApiPath}");
        }

        public async Task<bool> StartAsync() {
            try {
                if (_reactProcess == null || _reactProcess.HasExited) {
                    await StartReactAsync();
                }

                if (_webApiProcess == null || _webApiProcess.HasExited) {
                    await StartWebApiAsync();
                }

                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to start applications");
                return false;
            }
        }

        public async Task<bool> RebuildWebApiAsync() {
            try {
                await StopWebApiAsync();
                await StartWebApiAsync();
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to rebuild WebApi");
                return false;
            }
        }

        public async Task<bool> StopAsync() {
            try {
                await StopReactAsync();
                await StopWebApiAsync();
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to stop applications");
                return false;
            }
        }

        public ProcessStatus GetStatus() {
            _status.IsReactRunning = _reactProcess != null && !_reactProcess.HasExited;
            _status.IsWebApiRunning = _webApiProcess != null && !_webApiProcess.HasExited;
            return _status;
        }

        private async Task StartReactAsync() {
            if (_reactProcess != null && !_reactProcess.HasExited) {
                _logger.LogWarning("React process is already running");
                return;
            }

            _logger.LogInformation("Starting React application...");

            _reactProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = "/c npm run dev",
                    WorkingDirectory = _reactPath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            _reactProcess.Start();
            _status.ReactStartTime = DateTime.Now;
            _status.IsReactRunning = true;

            // プロセスが開始されるまで少し待機
            await Task.Delay(1000);
            _logger.LogInformation("React application started");
        }

        private async Task StartWebApiAsync() {
            if (_webApiProcess != null && !_webApiProcess.HasExited) {
                _logger.LogWarning("WebApi process is already running");
                return;
            }

            _logger.LogInformation("Starting WebApi application...");

            _webApiProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "dotnet",
                    Arguments = "run",
                    WorkingDirectory = _webApiPath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            _webApiProcess.Start();
            _status.WebApiStartTime = DateTime.Now;
            _status.IsWebApiRunning = true;

            // プロセスが開始されるまで少し待機
            await Task.Delay(1000);
            _logger.LogInformation("WebApi application started");
        }

        private async Task StopReactAsync() {
            if (_reactProcess == null || _reactProcess.HasExited) {
                _logger.LogWarning("React process is not running");
                _status.IsReactRunning = false;
                return;
            }

            _logger.LogInformation("Stopping React application...");

            try {
                // プロセスを停止するために関連するプロセスを検索して終了
                KillProcessAndChildren(_reactProcess.Id);
                await Task.Delay(500);
                _status.IsReactRunning = false;
                _status.ReactStartTime = null;
                _logger.LogInformation("React application stopped");
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to stop React application");
                throw;
            }
        }

        private async Task StopWebApiAsync() {
            if (_webApiProcess == null || _webApiProcess.HasExited) {
                _logger.LogWarning("WebApi process is not running");
                _status.IsWebApiRunning = false;
                return;
            }

            _logger.LogInformation("Stopping WebApi application...");

            try {
                // プロセスを停止するために関連するプロセスを検索して終了
                KillProcessAndChildren(_webApiProcess.Id);
                await Task.Delay(500);
                _status.IsWebApiRunning = false;
                _status.WebApiStartTime = null;
                _logger.LogInformation("WebApi application stopped");
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to stop WebApi application");
                throw;
            }
        }

        private void KillProcessAndChildren(int pid) {
            if (pid == 0) return;

            try {
                // 子プロセスを終了
                foreach (var process in Process.GetProcesses()) {
                    try {
                        using var mo = new ManagementObject($"Win32_Process.Handle='{process.Id}'");
                        var mo2 = mo.GetPropertyValue("ParentProcessId");

                        if (mo2 != null && mo2.ToString() == pid.ToString()) {
                            KillProcessAndChildren(process.Id);
                        }
                    } catch {
                        // エラーは無視
                    }
                }

                // 親プロセスを終了
                try {
                    var proc = Process.GetProcessById(pid);
                    if (!proc.HasExited) {
                        proc.Kill(true);
                    }
                } catch (ArgumentException) {
                    // プロセスが既に終了している場合は無視
                }
            } catch (Exception ex) {
                _logger.LogError(ex, $"Failed to kill process {pid}");
            }
        }

        public void Dispose() {
            StopAsync().Wait();
            _reactProcess?.Dispose();
            _webApiProcess?.Dispose();
        }
    }
}
