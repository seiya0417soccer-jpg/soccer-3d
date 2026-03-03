using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class BattleMainManager : MonoBehaviour
{
    public static BattleMainManager Instance;

    [Header("References")]
    public YushaBrain yusha;

    [Header("Buff Balance")]
    public float secondsPerBlock = 0.5f;
    public float speedPerBlock = 0.2f;

    private float bonusSpeed = 0f;
    private NavMeshAgent agent;

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

        agent = yusha.GetComponent<NavMeshAgent>();
    }

    public void OnBlocksDestroyed(int destroyedCount)
    {
        if (agent == null) return;

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
        if (agent != null)
            agent.speed = yusha.defaultSpeed + bonusSpeed;
    }
    public bool IsPaused { get; private set; }

    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }
}