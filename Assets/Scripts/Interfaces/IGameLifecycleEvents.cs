using System;

/// <summary>
/// ゲームのライフサイクル情報を公開する。
/// </summary>
public interface IGameLifecycleEvents
{
    // ゲームオーバーまたは制限時間終了時に通知する
    event Action GameEnded;

    // 現在ゲームプレイ中かどうか
    bool IsGameActive { get; }
}