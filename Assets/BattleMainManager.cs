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

    void UpdateSpeed()
    {
        yusha.UpdateSpeed(bonusSpeed);
    }
    public bool IsPaused { get; private set; }

    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }
}