using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 自動プレイ機能からパズルを操作するための窓口。
/// 盤面情報の取得と配置実行を公開し、具体的なゲーム処理から判断方式を分離する。
/// </summary>
public interface IPuzzleAutomationPort
{
    // 判断開始時点の盤面情報を取得し、通常操作を停止する
    bool TryBeginDecision(out PuzzleAutomationSnapshot snapshot);

    // 自動プレイ時の1マスごとの落下時間を設定する
    void SetAutomationFallStepInterval(float secondsPerCell);

    // 選択された配置を実行し、次のピースが描画されるまで待つ
    UniTask<bool> ExecutePlacementAsync(
        PuzzlePlacementCandidate candidate,
        CancellationToken cancellationToken);

    // 判断・配置処理が終了したら通常のゲーム進行に戻す
    void EndDecision();
}