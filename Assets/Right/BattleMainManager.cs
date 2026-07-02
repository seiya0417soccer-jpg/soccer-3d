using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMainManager : MonoBehaviour
{
    public static BattleMainManager Instance;

    [Header("References")]
    [SerializeField] private YushaBrain yusha;

    [Header("Buff Balance")]
    public float secondsPerBlock = 0.3f; // ブロック1個あたりのバフ持続時間
    public float speedPerBlock = 0.2f; // ブロック1個あたりの速度バフ量

    // 現在有効なバフエントリのリスト（amount, 残り時間）
    private List<BuffEntry> activeBuffs = new List<BuffEntry>();

    // デバフ中フラグ
    private bool isEKeyDebuffActive = false;
    private Coroutine eKeyDebuffCoroutine;

    // バフの総量（activeBuffsから都度計算）
    private float TotalBonusSpeed
    {
        get
        {
            float total = 0f;
            foreach (var b in activeBuffs) total += b.amount;
            return total;
        }
    }

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

    void Start()
    {
        if (yusha == null)
            Debug.LogError("Yusha がセットされていません！");
    }

    // ==================================================
    // 通常ブロック破壊時（速度バフ付与）
    // ==================================================
    public void OnBlocksDestroyed(int destroyedCount)
    {
        float duration = destroyedCount * secondsPerBlock;
        float speedAmount = destroyedCount * speedPerBlock;
        StartCoroutine(SpeedBuffCoroutine(duration, speedAmount));
    }

    // バフを1件追加 → 時間経過後に除去
    IEnumerator SpeedBuffCoroutine(float duration, float amount)
    {
        var entry = new BuffEntry(amount);
        activeBuffs.Add(entry);
        ApplySpeed(); // バフ追加後に速度反映

        yield return new WaitForSeconds(duration);

        activeBuffs.Remove(entry);
        ApplySpeed(); // バフ除去後に速度反映
    }

    // ==================================================
    // Eキー爆弾デバフ（一定時間停止）
    // ==================================================
    public void ApplyEKeyDebuff(float duration)
    {
        if (eKeyDebuffCoroutine != null)
            StopCoroutine(eKeyDebuffCoroutine);
        eKeyDebuffCoroutine = StartCoroutine(EKeyDebuffCoroutine(duration));
    }

    IEnumerator EKeyDebuffCoroutine(float duration)
    {
        if (yusha == null) yield break;

        isEKeyDebuffActive = true;
        yusha.UpdateSpeed(-yusha.defaultSpeed); // 勇者を停止

        yield return new WaitForSeconds(duration);

        isEKeyDebuffActive = false;
        eKeyDebuffCoroutine = null;
        ApplySpeed(); // デバフ解除後、現在のバフ量を反映
    }

    // ==================================================
    // 現在のバフ量を勇者に反映
    // デバフ中は無視（デバフ解除時に ApplySpeed が呼ばれる）
    // ==================================================
    void ApplySpeed()
    {
        if (yusha == null) return;
        if (isEKeyDebuffActive) return; // デバフ中はバフを反映しない
        yusha.UpdateSpeed(TotalBonusSpeed);
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