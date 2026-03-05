using System.Collections;
using UnityEngine;

public class BattleMainManager : MonoBehaviour
{
    public static BattleMainManager Instance;

    [Header("References")]
    [SerializeField] private YushaBrain yusha;

    [Header("Buff Balance")]
    public float secondsPerBlock = 0.3f;
    public float speedPerBlock = 0.2f;

    private float bonusSpeed = 0f;
    private Coroutine eKeyDebuffCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (yusha == null)
        {
            Debug.LogError("Yusha がセットされていません！");
            return;
        }
    }

    // 通常のバフ（破壊ブロック数に応じて）
    public void OnBlocksDestroyed(int destroyedCount)
    {
        float duration = destroyedCount * secondsPerBlock;
        float speedAmount = destroyedCount * speedPerBlock;

        StartCoroutine(SpeedBuff(duration, speedAmount));
    }

    IEnumerator SpeedBuff(float duration, float amount)
    {
        bonusSpeed += amount;
        UpdateSpeed();

        yield return new WaitForSeconds(duration);

        bonusSpeed -= amount;
        UpdateSpeed();
    }

    // Eキー爆弾用の速度デバフ（一定時間勇者を止める）
    public void ApplyEKeyDebuff(float duration)
    {
        if (eKeyDebuffCoroutine != null)
            StopCoroutine(eKeyDebuffCoroutine);

        eKeyDebuffCoroutine = StartCoroutine(EKeyDebuffCoroutine(duration));
    }

    private IEnumerator EKeyDebuffCoroutine(float duration)
    {
        if (yusha == null)
            yield break;

        // 現在のバフを保持
        float currentBonus = bonusSpeed;

        // 勇者を停止（バフは維持するので基本速度＋バフを一時0にする）
        yusha.UpdateSpeed(-yusha.defaultSpeed);

        yield return new WaitForSeconds(duration);

        // デバフ解除、元の速度に戻す
        yusha.UpdateSpeed(currentBonus);
        eKeyDebuffCoroutine = null;
    }

    void UpdateSpeed()
    {
        if (yusha != null)
            yusha.UpdateSpeed(bonusSpeed);
    }

    public bool IsPaused { get; private set; }

    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }
}