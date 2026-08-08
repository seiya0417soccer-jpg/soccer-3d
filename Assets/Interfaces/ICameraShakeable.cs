using R3;

/// <summary>
/// ICameraShakeable.cs
/// カメラシェイク操作の抽象インターフェース
/// 
/// - カメラ演出の責務をIBattleFieldから分離する
///   （バトルの責務とカメラ演出の責務を混在させない）
/// - GameFlowManagerはこのInterfaceを通してカメラシェイクを操作する
///   → YushaBrain具体型に依存しない設計にする
/// - 将来カメラシェイクの実装を変えても
///   GameFlowManagerは変更不要（拡張性の向上）
/// </summary>
public interface ICameraShakeable
{
    // カメラシェイクを強制停止して以降のシェイクも禁止する
    // ゲームオーバー・フィニッシュ時に呼ぶ
    void StopCameraShake();

    // カメラシェイクの禁止を解除する
    // もう一度プレイ・タイトル復帰時に呼ぶ
    void EnableCameraShake();
}