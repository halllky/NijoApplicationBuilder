using System;

namespace Nijo.WebService.DemoMode;

/// <summary>
/// 排他ロックの現在の保持状況。
/// </summary>
/// <param name="OwnerClientId">ロックを取得したクライアントのID</param>
/// <param name="Reason">ロックの理由(画面表示用。例: "AIがスキーマを編集中")</param>
/// <param name="AcquiredAtUtc">ロック取得時刻</param>
public record DemoLockInfo(string OwnerClientId, string Reason, DateTime AcquiredAtUtc);

/// <summary>
/// AIチャットの1メッセージ。
/// </summary>
/// <param name="Role">"user" または "assistant"</param>
/// <param name="Content">本文</param>
/// <param name="CreatedAtUtc">作成時刻</param>
public record DemoChatMessage(string Role, string Content, DateTime CreatedAtUtc);
