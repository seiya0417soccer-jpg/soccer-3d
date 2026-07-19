using R3;

/// <summary>
/// IBattleField.cs
/// 左画面（バトル側）の抽象インターフェース
/// 
/// - 実装を差し替えることで別のバトル形式に変更できる
/// - YushaBrainはこのInterfaceを実装する
/// - パズル側はこのInterfaceを通してバトル側を知る
/// - R3のObservableでイベントを発火する（疎結合・読み取り専用公開）
/// </summary>
public interface IBattleField
{
    // 初期位置にリセットする
    void ResetPosition();

    // 速度バフを適用する
    void UpdateSpeed(float bonusSpeed);

    // Eキーデバフを適用する
    void ApplyEKeyDebuff(float duration);

    // 発光強度を設定する（バフ時）
    void SetEmission(float intensity);

    // デバフ時の発光色を設定する
    void SetDebuffEmission(bool isDebuff);

    // 敵を倒した時に発火するObservable（スコア加算用）
    Observable<Unit> OnEnemyDefeated { get; }
}