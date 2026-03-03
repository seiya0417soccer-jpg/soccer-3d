using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

/// <summary>
/// テトリス拡張ロジック完全版
/// ・爆弾時止め
/// ・バフ予約（最大倍率適用）
/// ・オーバードライブ
/// ・デバフ停止
/// DropPuzzleBattleは一切変更しない
/// </summary>
public class DropLogicExtension : MonoBehaviour
{
    private DropPuzzleBattle core;
    private BattleMainManager main;

    private NavMeshAgent agent;

    private const int Width = 13;
    private const int Height = 22;

    private int[,] localField;

    private bool isBombProcessing = false;
    private int reservedDestroyed = 0;

    void Awake()
    {
        core = GetComponent<DropPuzzleBattle>();
        main = BattleMainManager.Instance;

        if (main != null && main.yusha != null)
            agent = main.yusha.GetComponent<NavMeshAgent>();

        localField = new int[Height, Width];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isBombProcessing)
        {
            StartCoroutine(BombSequence());
        }
    }

    // =====================================================
    // ■ 爆弾メインシーケンス
    // =====================================================
    IEnumerator BombSequence()
    {
        isBombProcessing = true;

        // -------------------------
        // ① 完全時止め
        // -------------------------
        main.SetPause(true);
        if (agent != null)
            agent.isStopped = true;

        SyncFromCore();

        // ★ ここで本来は爆発処理を書く
        // 今回は「予約テスト用」に全体スキャンだけ
        reservedDestroyed = CountAllBlocks();

        yield return new WaitForSecondsRealtime(1f);

        SyncToCore();

        // -------------------------
        // ② 再開と同時に最大倍率適用
        // -------------------------
        ApplyReservedBuff();

        main.SetPause(false);
        if (agent != null)
            agent.isStopped = false;

        // -------------------------
        // ③ 爆弾後デバフ（2.5秒停止）
        // -------------------------
        StartCoroutine(StunPenalty());

        isBombProcessing = false;
    }

    // =====================================================
    // ■ デバフ停止
    // =====================================================
    IEnumerator StunPenalty()
    {
        if (agent == null) yield break;

        agent.isStopped = true;
        float timer = 2.5f;

        while (timer > 0f)
        {
            if (CheckLineCleared()) // 1列でも消えたら解除
                break;

            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        agent.isStopped = false;
    }

    // =====================================================
    // ■ 最大倍率適用ロジック
    // =====================================================
    void ApplyReservedBuff()
    {
        if (reservedDestroyed <= 0) return;

        // 54個以上 → オーバードライブ
        if (reservedDestroyed >= 54)
        {
            StartCoroutine(OverDrive());
            return;
        }

        float duration = reservedDestroyed * main.secondsPerBlock;
        float speedAmount = reservedDestroyed * main.speedPerBlock;

        main.StartCoroutine(
            ManualBuff(duration, speedAmount)
        );
    }

    IEnumerator ManualBuff(float duration, float amount)
    {
        agent.speed += amount;
        yield return new WaitForSeconds(duration);
        agent.speed -= amount;
    }

    IEnumerator OverDrive()
    {
        float original = agent.speed;
        agent.speed = 12f;

        yield return new WaitForSeconds(10f);

        agent.speed = original;
    }

    // =====================================================
    // ■ 盤面同期
    // =====================================================
    void SyncFromCore()
    {
        FieldInfo fieldInfo =
            typeof(DropPuzzleBattle).GetField("field",
            BindingFlags.NonPublic | BindingFlags.Instance);

        int[,] coreField = (int[,])fieldInfo.GetValue(core);

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                localField[y, x] = coreField[y, x];
    }

    void SyncToCore()
    {
        FieldInfo fieldInfo =
            typeof(DropPuzzleBattle).GetField("field",
            BindingFlags.NonPublic | BindingFlags.Instance);

        fieldInfo.SetValue(core, localField);
    }

    // =====================================================
    // ■ 補助
    // =====================================================
    int CountAllBlocks()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (localField[y, x] != 0)
                    count++;
        return count;
    }

    bool CheckLineCleared()
    {
        for (int y = 0; y < Height; y++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (localField[y, x] == 0)
                {
                    full = false;
                    break;
                }
            }
            if (full) return true;
        }
        return false;
    }
}