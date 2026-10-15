/// <summary>
/// 自動プレイの現在の状態。
/// </summary>
public enum JevAutoPlayStatus
{
    Idle,           // 初期状態
    WaitingForBoard,// 盤面の準備待ち
    Judging,        // Jevの判断中
    Executing,      // 移動・着地・盤面処理中
    Stopping,       // 停止要求後、処理の終了待ち
    Stopped,        // 停止済み
    Error           // エラーによる終了
}