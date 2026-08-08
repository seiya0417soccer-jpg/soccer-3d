/// <summary>
/// RankingState.cs
/// ランキング機能の状態を表すクラス群
/// 
/// - otameshiで検証したStateパターンをsoccer-3dに適用した
/// - LoadingState・SuccessState・ErrorStateの3状態で管理する
/// - RankingViewModelがStateを更新し
///   RankingView・RankingSubmitUIがStateを購読して表示を切り替える
/// </summary>
public abstract class RankingState { }

/// <summary>
/// 読込中・送信中の状態
/// </summary>
public class LoadingState : RankingState { }

/// <summary>
/// 取得・送信成功の状態
/// </summary>
public class SuccessState : RankingState { }

/// <summary>
/// 取得・送信失敗の状態
/// エラーメッセージを持つ
/// </summary>
public class ErrorState : RankingState
{
    public string Message { get; }

    public ErrorState(string message)
    {
        Message = message;
    }
}