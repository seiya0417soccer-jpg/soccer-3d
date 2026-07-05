using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// BattleMainManager.cs
/// パズル消去数→勇者スピードバフ計算・デバフ管理
/// 
/// - ブロック破壊数に応じて勇者にスピードバフを付与
/// - EキーBomb発動時に勇者にデバフを付与
/// - VContainerでYushaBrainを注入（SerializeField依存をなくす）
/// </summary>
public class BattleMainManager : MonoBehaviour
{
    // シングルトン：他スクリプトからBattleMainManager.Instanceでアクセス
    public static BattleMainManager Instance;

    [Header("Buff Balance")]
    [SerializeField] private float _secondsPerBlock = 0.3f; // ブロック1個あたりのバフ持続時間
    [SerializeField] private float _speedPerBlock = 0.2f;   // ブロック1個あたりの速度バフ量

    // VContainerで注入される依存クラス
    private YushaBrain _yusha;

    // 現在有効なバフエントリのリスト
    private List<BuffEntry> _activeBuffs = new List<BuffEntry>();

    // デバフ中フラグ
    private bool _isEKeyDebuffActive = false;
    private Coroutine _eKeyDebuffCoroutine;

    // バフの総量（activeBuffsから都度計算）
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
    // ==================================================
    [Inject]
    public void Construct(YushaBrain yusha)
    {
        _yusha = yusha;
    }

    // ==================================================
    // Awake: Singleton登録
    // ==================================================
    void Awake()
    {
        // 既にインスタンスが存在する場合は自分を破棄する（重複防止）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 最初の1つだけ登録する
        Instance = this;
    }

    // ==================================================
    // Start: 初期化確認
    // ==================================================
    void Start()
    {
        if (_yusha == null)
            Debug.LogError("BattleMainManager: YushaBrainが注入されていません！");
    }

    // ==================================================
    // 通常ブロック破壊時（速度バフ付与）
    // ==================================================
    public void OnBlocksDestroyed(int destroyedCount)
    {
        float duration = destroyedCount * _secondsPerBlock;
        float speedAmount = destroyedCount * _speedPerBlock;
        StartCoroutine(SpeedBuffCoroutine(duration, speedAmount));
    }

    // バフを1件追加 → 時間経過後に除去
    IEnumerator SpeedBuffCoroutine(float duration, float amount)
    {
        var entry = new BuffEntry(amount);
        _activeBuffs.Add(entry);
        ApplySpeed(); // バフ追加後に速度反映
        yield return new WaitForSeconds(duration);
        _activeBuffs.Remove(entry);
        ApplySpeed(); // バフ除去後に速度反映
    }

    // ==================================================
    // Eキー爆弾デバフ（一定時間停止）
    // ==================================================
    public void ApplyEKeyDebuff(float duration)
    {
        if (_eKeyDebuffCoroutine != null)
            StopCoroutine(_eKeyDebuffCoroutine);

        // YushaBrainのデバフも同時に適用する
        _yusha?.ApplyEKeyDebuff(duration);

        _eKeyDebuffCoroutine = StartCoroutine(EKeyDebuffCoroutine(duration));
    }

    IEnumerator EKeyDebuffCoroutine(float duration)
    {
        if (_yusha == null) yield break;

        _isEKeyDebuffActive = true;
        _yusha.UpdateSpeed(-_yusha.DefaultSpeed); // 勇者を停止
        yield return new WaitForSeconds(duration);
        _isEKeyDebuffActive = false;
        _eKeyDebuffCoroutine = null;
        ApplySpeed(); // デバフ解除後、現在のバフ量を反映
    }

    // ==================================================
    // 現在のバフ量を勇者に反映
    // デバフ中は無視（デバフ解除時にApplySpeedが呼ばれる）
    // ==================================================
    void ApplySpeed()
    {
        if (_yusha == null) return;
        if (_isEKeyDebuffActive) return; // デバフ中はバフを反映しない
        _yusha.UpdateSpeed(TotalBonusSpeed);
    }

    // ==================================================
    // 一時停止
    // ==================================================
    public bool IsPaused { get; private set; }
    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    // ==================================================
    // バフエントリ（参照で管理するためクラスを使用）
    // ==================================================
    private class BuffEntry
    {
        public float amount;
        public BuffEntry(float amount) { this.amount = amount; }
    }
}