/// <summary>
/// 自動プレイ機能からパズルを操作するための窓口。
///
/// Jevなどの判断方式には依存せず、
/// 盤面の取得・判断開始・配置実行だけを公開する。
/// </summary>
public interface IPuzzleAutomationPort
{
    /// 判断を開始し、現在の盤面情報を取得する。
    /// 判断中は自動落下と手動入力を停止する。
    bool TryBeginDecision(out PuzzleAutomationSnapshot snapshot);

    /// 現在のピースが生成された回数を取得する。
    /// 新しいピースが生成されるたびに値が増加する。
    int PieceGeneration { get; }

    /// 浮き島の落下や連鎖処理など、盤面の解決処理中かどうか。
    bool IsBoardResolving { get; }

    /// 自動プレイ時の1マスごとの落下時間を設定する。
    void SetAutomationFallStepInterval(float secondsPerCell);

    /// 選択された配置を再検証して実行する。
    bool TryApplyPlacement(PuzzlePlacementCandidate candidate);

    /// 判断を終了して通常のゲーム進行に戻す。
    void EndDecision();
}