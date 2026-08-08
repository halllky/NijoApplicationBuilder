using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// ユーザーのチャットメッセージを headless の claude CLI (`claude -p`) に渡し、
/// 出力をSignalR経由で全クライアントへストリーミングする。
/// </summary>
public class ClaudeAgentService {

    private const int MAX_HISTORY = 100;

    private readonly DemoModeOptions _options;
    private readonly IHubContext<DemoHub, IDemoHubClient> _hub;
    private readonly ILogger<ClaudeAgentService> _logger;

    private readonly List<DemoChatMessage> _history = new();
    private readonly object _historyLock = new();
    private string? _sessionId;
    private Process? _runningProcess;

    public ClaudeAgentService(DemoModeOptions options, IHubContext<DemoHub, IDemoHubClient> hub, ILogger<ClaudeAgentService> logger) {
        _options = options;
        _hub = hub;
        _logger = logger;
    }

    public IReadOnlyList<DemoChatMessage> History {
        get {
            lock (_historyLock) {
                return _history.ToList();
            }
        }
    }

    /// <summary>
    /// nijo.xml が変更されたか(=自動generateすべきか)を呼び出し側が判断するためのイベント。
    /// </summary>
    public event Func<Task>? SchemaMayHaveChanged;

    public async Task RunAsync(string userMessage) {
        AppendHistory("user", userMessage);
        await _hub.Clients.All.ChatMessageAppended(new DemoChatMessage("user", userMessage, DateTime.UtcNow));

        var process = new Process();
        process.StartInfo.FileName = "claude";
        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add(userMessage);
        process.StartInfo.ArgumentList.Add("--output-format");
        process.StartInfo.ArgumentList.Add("stream-json");
        process.StartInfo.ArgumentList.Add("--verbose");
        process.StartInfo.ArgumentList.Add("--dangerously-skip-permissions");
        process.StartInfo.ArgumentList.Add("--max-turns");
        process.StartInfo.ArgumentList.Add("30");
        if (_sessionId != null) {
            process.StartInfo.ArgumentList.Add("--resume");
            process.StartInfo.ArgumentList.Add(_sessionId);
        }
        process.StartInfo.WorkingDirectory = _options.WorkspaceRoot;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        var assistantText = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) => {
            if (e.Data == null) return;
            HandleStreamJsonLine(e.Data, assistantText);
        };
        process.ErrorDataReceived += (_, e) => {
            if (e.Data == null) return;
            _ = _hub.Clients.All.ProcessOutput("claude", e.Data);
        };

        _runningProcess = process;
        try {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        } finally {
            _runningProcess = null;
        }

        var finalText = assistantText.ToString();
        if (finalText.Length > 0) {
            AppendHistory("assistant", finalText);
            await _hub.Clients.All.ChatMessageAppended(new DemoChatMessage("assistant", finalText, DateTime.UtcNow));
        }

        if (SchemaMayHaveChanged != null) {
            await SchemaMayHaveChanged.Invoke();
        }
    }

    public void Cancel() {
        if (_runningProcess != null) {
            _runningProcess.EnsureKill();
        }
    }

    public void ResetConversation() {
        lock (_historyLock) {
            _history.Clear();
        }
        _sessionId = null;
    }

    private void HandleStreamJsonLine(string line, System.Text.StringBuilder assistantText) {
        try {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (root.TryGetProperty("session_id", out var sessionIdProp)) {
                _sessionId = sessionIdProp.GetString();
            }

            var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            if (type == "assistant" && root.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array) {
                foreach (var block in content.EnumerateArray()) {
                    if (block.TryGetProperty("type", out var blockType) && blockType.GetString() == "text"
                        && block.TryGetProperty("text", out var textProp)) {
                        var chunk = textProp.GetString() ?? "";
                        assistantText.Append(chunk);
                        _ = _hub.Clients.All.ChatStreamChunk(chunk);
                    } else {
                        // ツール使用など、テキスト以外のブロックはプロセスログとして要約表示する
                        _ = _hub.Clients.All.ProcessOutput("claude", block.ToString());
                    }
                }
            } else {
                _ = _hub.Clients.All.ProcessOutput("claude", line);
            }
        } catch (JsonException) {
            // stream-json以外の行(警告メッセージ等)はそのままログとして流す
            _ = _hub.Clients.All.ProcessOutput("claude", line);
        }
    }

    private void AppendHistory(string role, string content) {
        lock (_historyLock) {
            _history.Add(new DemoChatMessage(role, content, DateTime.UtcNow));
            if (_history.Count > MAX_HISTORY) {
                _history.RemoveAt(0);
            }
        }
    }
}
