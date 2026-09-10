/// <summary>
/// GameConstants.cs
/// ゲーム全体で使う定数をまとめたクラス
/// 
/// - 複数のスクリプトで使う文字列・数値を一箇所で管理する
/// - 同じ文字列を各クラスに直接書くと、変更時に修正漏れが起きる
///   → ここに集約することで変更箇所を1箇所にする（保守性の向上）
/// - チーム開発で「この定数どこで定義されてる？」という問題を防ぐ
/// </summary>
public static class GameConstants
{
    // ==================================================
    // PlayerPrefsキー定数
    // ここを変えるだけで全箇所に反映される
    // ==================================================

    // ResultManager・ResetBestScoreManagerで共通して使う
    public const string BestScoreKey = "BestScore";

    // RankingSubmitUI・ResetBestScoreManagerで共通して使う
    // 自己ベスト削除時に名前もリセットするためここで一本管理する
    public const string PlayerNameKey = "PlayerName";
}