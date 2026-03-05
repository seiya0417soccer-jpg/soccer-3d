using System.Collections;
using UnityEngine;

public class BattleMainManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static BattleMainManager Instance;

    [Header("References")]
    [SerializeField] private YushaBrain yusha; // 勇者の参照

    [Header("Buff Balance")]
    public float secondsPerBlock = 0.3f; // 破壊ブロック1個あたりのバフ持続時間
    public float speedPerBlock = 0.2f;   // 破壊ブロック1個あたりの速度バフ量

    // 現在のバフ量
    private float bonusSpeed = 0f;

    // Eキー爆弾デバフ用コルーチン参照
    private Coroutine eKeyDebuffCoroutine;

    void Awake()
    {
        // シングルトン登録
        Instance = this;
    }

    void Start()
    {
        // 勇者参照が未設定なら警告
        if (yusha == null)
        {
            Debug.LogError("Yusha がセットされていません！");
            return;
        }
    }

    // 通常ブロック破壊時に呼ばれる処理（速度バフ付与）
    public void OnBlocksDestroyed(int destroyedCount)
    {
        float duration = destroyedCount * secondsPerBlock; // 持続時間計算
        float speedAmount = destroyedCount * speedPerBlock; // 速度量計算

        StartCoroutine(SpeedBuff(duration, speedAmount));
    }

    // 一定時間速度バフを適用
    IEnumerator SpeedBuff(float duration, float amount)
    {
        bonusSpeed += amount; // バフ加算
        UpdateSpeed();         // 勇者速度更新

        yield return new WaitForSeconds(duration); // バフ持続待機

        bonusSpeed -= amount; // バフ解除
        UpdateSpeed();        // 勇者速度更新
    }

    // Eキー爆弾用デバフ（一定時間勇者を停止）
    public void ApplyEKeyDebuff(float duration)
    {
        // すでにデバフ中なら停止して再開始
        if (eKeyDebuffCoroutine != null)
            StopCoroutine(eKeyDebuffCoroutine);

        eKeyDebuffCoroutine = StartCoroutine(EKeyDebuffCoroutine(duration));
    }

    private IEnumerator EKeyDebuffCoroutine(float duration)
    {
        if (yusha == null)
            yield break;

        // 現在のバフ量を保持
        float currentBonus = bonusSpeed;

        // 勇者を停止（基本速度+バフを0にして一時停止）
        yusha.UpdateSpeed(-yusha.defaultSpeed);

        yield return new WaitForSeconds(duration); // デバフ時間待機

        // デバフ解除、元の速度に戻す
        yusha.UpdateSpeed(currentBonus);
        eKeyDebuffCoroutine = null;
    }

    // 現在のバフ量を勇者に反映
    void UpdateSpeed()
    {
        if (yusha != null)
            yusha.UpdateSpeed(bonusSpeed);
    }

    // ゲーム一時停止状態の取得
    public bool IsPaused { get; private set; }

    // ゲーム一時停止・解除
    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f; // Unity時間を停止／再開
    }
}