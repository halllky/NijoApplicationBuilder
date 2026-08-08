using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 共有デモ環境そのものを表す抽象。「環境をリセットする」という、手動リセットボタンと
/// <see cref="IdleResetWatchdog"/> の両方から呼ばれる。
/// </summary>
public class DemoEnvironment {

    private readonly DemoModeOptions _options;
    private readonly DemoLock _lock;
    private readonly Demo101App _demo101App;
    private readonly ClaudeAgent _claudeAgent;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<DemoEnvironment> _logger;

    public DemoEnvironment(
        DemoModeOptions options,
        DemoLock @lock,
        Demo101App demo101App,
        ClaudeAgent claudeAgent,
        IHubContext<DemoHub, IDemoHubClient> hub,
        ILogger<DemoEnvironment> logger) {
        _options = options;
        _lock = @lock;
        _demo101App = demo101App;
        _claudeAgent = claudeAgent;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// 共有デモ環境を pristine 状態(イメージビルド時にgit commitした状態)へ戻す。
    /// 手動リセットボタンと <see cref="IdleResetWatchdog"/> の両方から呼ばれる。
    /// </summary>
    public async Task<bool> ResetAsync(string clientId) {
        var handle = await _lock.TryAcquireAsync(clientId, "環境をリセット中");
        if (handle == null) return false;

        try {
            _demo101App.Stop();

            // pristine状態(イメージビルド時にコミットしたコミット)へ強制的に戻す。
            // node_modules/bin/obj はビルドキャッシュとして残し、再ビルドの時間を短縮する。
            var checkoutExitCode = await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("checkout");
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(".");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => _logger.LogInformation("[git checkout] {line}", line), TimeSpan.FromMinutes(1));
            if (checkoutExitCode != 0) {
                _logger.LogError("リセット中の git checkout が失敗しました (exit code = {code})。ワークスペースが復元されていない可能性があります。", checkoutExitCode);
            }

            var cleanExitCode = await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("clean");
                psi.ArgumentList.Add("-fd");
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("node_modules");
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("**/bin");
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("**/obj");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => _logger.LogInformation("[git clean] {line}", line), TimeSpan.FromMinutes(1));
            if (cleanExitCode != 0) {
                _logger.LogError("リセット中の git clean が失敗しました (exit code = {code})。ワークスペースが復元されていない可能性があります。", cleanExitCode);
            }

            _claudeAgent.ResetConversation();

            // pristine状態(イメージビルド時のコミット)にはビルド済みのpublish成果物/wwwrootも
            // 含めてコミットしてあるため、通常はgit checkoutで復元されておりそのまま起動できる
            // (StartAsyncは成果物が無いときだけ自動的にフルビルドする)。
            await _demo101App.StartAsync();
        } finally {
            await handle.DisposeAsync();
        }

        await _hub.Clients.All.ForceReload("環境がリセットされました");
        return true;
    }

    /// <summary>
    /// ワークスペースがpristine状態から変化しているか(git status --porcelain が空でないか)を確認する。
    /// 変化が無ければリセットしても意味が無いのでスキップする。
    ///
    /// DEBUG.sqlite3・WebApi.Log は実行時に必ず作られる untracked ファイル/ディレクトリのため
    /// (Program.cs起動時シード等)、これらを含めると常に「変化あり」判定になってしまう。
    /// pathspecで除外し、実際のソース変更(AIによるnijo.xml編集等)の有無だけを見る。
    /// </summary>
    public async Task<bool> IsDirtyAsync(CancellationToken ct) {
        var isDirty = false;
        try {
            await ProcessExtension.ExecuteProcessAsync(psi => {
                psi.FileName = "git";
                psi.ArgumentList.Add("status");
                psi.ArgumentList.Add("--porcelain");
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(".");
                psi.ArgumentList.Add(":!DEBUG.sqlite3");
                psi.ArgumentList.Add(":!WebApi.Log");
                psi.WorkingDirectory = _options.WorkspaceRoot;
            }, (std, line) => {
                if (std == ProcessExtension.E_STD.StdOut && !string.IsNullOrWhiteSpace(line)) {
                    isDirty = true;
                }
            }, TimeSpan.FromSeconds(30));
        } catch (Exception ex) {
            _logger.LogWarning(ex, "git status の確認に失敗しました。安全側に倒してリセットを実行します。");
            return true;
        }
        return isDirty;
    }
}
