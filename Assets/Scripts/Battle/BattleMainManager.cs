using R3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// BattleMainManager.cs
/// パズル消去数→勇者スピードバフ計算・デバフ管理
/// 
/// - ブロック破壊数に応じて勇者にスピードバフを付与する
/// - EキーBomb発動時に勇者にデバフを付与する
/// - YushaBrain具体型ではなくIBattleField経由で依存する
///   → 将来バトル形式を差し替えてもBattleMainManagerは変更不要（拡張性の向上）
/// - デバフ終了通知をIBattleField.OnDebuffFinishedで受け取る
///   → 速度の最終決定をBattleMainManagerに一本化する設計にした
/// - BattleSettingSOでパラメーターを管理する（プランナーが調整可能）
/// </summary>
public class BattleMainManager : MonoBehaviour
{
    // プランナーが調整できるパラメーターをSOで管理
    [SerializeField] private BattleSettingSO _battleSettingSO;

    // IBattleField経由で注入される（YushaBrain具体型に依存しない）
    // Interfaceで受け取ることでバトル形式を差し替えても影響を受けない
    private IBattleField _battleField;

    // IPuzzleFieldを購読する（VContainerで注入される）
    private IPuzzleField _puzzleField;

    // 現在有効なバフエントリのリスト
    // クラスで管理することで参照による追加・削除が正確に行える
    private List<BuffEntry> _activeBuffs = new List<BuffEntry>();

    // バフの総量（_activeBuffsから都度計算する）
    private float TotalBonusSpeed
    {
        get
        {
            float total = 0f;
            foreach (var b in _activeBuffs) total += b.amount;
            return total;
        }
    }

    // ==================================================
    // Inject: VContainerから依存を注入される
    // YushaBrain具体型ではなくIBattleField経由で受け取る
    // → バトル形式を差し替えてもここは変更不要
    // ==================================================
    [Inject]
    public void Construct(IBattleField battleField, IPuzzleField puzzleField)
    {
        _battleField = battleField;
        _puzzleField = puzzleField;
    }

    // ==================================================
    // Start: 購読の初期化
    // ==================================================
    void Start()
    {
        if (_battleField == null)
            Debug.LogError("BattleMainManager: IBattleFieldが注入されていません！");
        if (_battleSettingSO == null)
            Debug.LogError("BattleMainManager: BattleSettingSOがセットされていません！");

        // ブロック消去数を購読してバフを付与する
        // AddTo(this)でBattleMainManager破棄時に自動で購読解除する（メモリリーク防止）
        _puzzleField.OnBlocksDestroyed
            .Subscribe(count => OnBlocksDestroyed(count))
            .AddTo(this);

        // EキーBomb爆発を購読してデバフを付与する
        // AddTo(this)でBattleMainManager破棄時に自動で購読解除する（メモリリーク防止）
        _puzzleField.OnEKeyBombExploded
            .Subscribe(_ => _battleField?.ApplyEKeyDebuff(_battleSettingSO.EKeyDebuffDuration))
            .AddTo(this);

        // デバフ終了通知を購読して現在のバフ量を再適用する
        // 速度の最終決定をBattleMainManagerに一本化するための設計
        // AddTo(this)でBattleMainManager破棄時に自動で購読解除する（メモリリーク防止）
        _battleField.OnDebuffFinished
            .Subscribe(_ => ApplySpeed())
            .AddTo(this);
    }

    // ==================================================
    // OnBlocksDestroyed: 通常ブロック破壊時（速度バフ付与）
    // 消去ブロック数に応じてバフ時間と速度を計算する
    // ==================================================
    public void OnBlocksDestroyed(int destroyedCount)
    {
        float duration = destroyedCount * _battleSettingSO.SecondsPerBlock;
        float speedAmount = destroyedCount * _battleSettingSO.SpeedPerBlock;
        StartCoroutine(SpeedBuffCoroutine(duration, speedAmount));
    }

    // ==================================================
    // SpeedBuffCoroutine: バフを1件追加して時間経過後に除去する
    // バフ追加・除去のタイミングで速度を反映する
    // ==================================================
    IEnumerator SpeedBuffCoroutine(float duration, float amount)
    {
        var entry = new BuffEntry(amount);
        _activeBuffs.Add(entry);
        ApplySpeed(); // バフ追加後に速度を反映する
        yield return new WaitForSeconds(duration);
        _activeBuffs.Remove(entry);
        ApplySpeed(); // バフ除去後に速度を反映する
    }

    // ==================================================
    // ApplySpeed: 現在のバフ量を勇者に反映する
    // デバフ中かどうかの判断はIBattleField.UpdateSpeed()内で行う
    // BattleMainManagerは「バフ量を渡すだけ」に責務を絞っている
    // ==================================================
    void ApplySpeed()
    {
        if (_battleField == null) return;

        float bonusSpeed = TotalBonusSpeed;
        _battleField.UpdateSpeed(bonusSpeed);

        // バフ量に応じて発光強度を変える（最大3倍速で最大発光）
        float emissionIntensity = Mathf.Clamp01(bonusSpeed / 3f);
        _battleField.SetEmission(emissionIntensity);
    }

    // ==================================================
    // SetPause: 一時停止・再開
    // ==================================================
    public bool IsPaused { get; private set; }
    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    // ==================================================
    // BuffEntry: バフエントリ
    // structではなくclassで管理することで
    // リストへの追加・削除が参照で正確に行える
    // ==================================================
    private class BuffEntry
    {
        public float amount;
        public BuffEntry(float amount) { this.amount = amount; }
    }
}