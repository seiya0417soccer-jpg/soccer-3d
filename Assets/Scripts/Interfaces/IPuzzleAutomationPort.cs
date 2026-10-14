/// <summary>
/// 自動プレイ機能からパズルを操作するための窓口。
///
/// Jevなどの判断方式には依存せず、
/// 盤面の取得・判断開始・配置実行だけを公開する。
/// </summary>
public interface IPuzzleAutomationPort
{
    /// <summary>
    /// 判断を開始し、現在の盤面情報を取得する。
    /// 判断中は自動落下と手動入力を停止する。
    /// </summary>
    bool TryBeginDecision(out PuzzleAutomationSnapshot snapshot);

    /// <summary>
    /// 現在のピースが生成された回数を取得する。
    /// 新しいピースが生成されるたびに値が増加する。
    /// </summary>
    int PieceGeneration { get; }

    /// <summary>
    /// 自動プレイ時の1マスごとの落下時間を設定する。
    /// </summary>
    void SetAutomationFallStepInterval(float secondsPerCell);

    /// <summary>
    /// 選択された配置を再検証して実行する。
    /// </summary>
    bool TryApplyPlacement(PuzzlePlacementCandidate candidate);

    /// <summary>
    /// 判断を終了して通常のゲーム進行に戻す。
    /// </summary>
    void EndDecision();
}