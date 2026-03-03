using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

public class DropLogicExtension : MonoBehaviour
{
    private DropPuzzleBattle core;
    private BattleMainManager main;
    private NavMeshAgent agent;

    private const int Width = 13;
    private const int Height = 22;

    private int[,] field;

    private bool bombNextPiece = false;
    private bool isProcessing = false;

    private int reservedDestroyed = 0;

    void Awake()
    {
        core = GetComponent<DropPuzzleBattle>();
        main = BattleMainManager.Instance;

        if (main != null && main.yusha != null)
            agent = main.yusha.GetComponent<NavMeshAgent>();

        // field取得（1回だけ）
        FieldInfo f =
            typeof(DropPuzzleBattle).GetField("field",
            BindingFlags.NonPublic | BindingFlags.Instance);

        field = (int[,])f.GetValue(core);
    }

    void Start()
    {
        core.OnPieceFixed += HandlePieceFixed;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !bombNextPiece && !isProcessing)
        {
            bombNextPiece = true;
        }
    }

    // =====================================================
    // ■ ピース確定時
    // =====================================================
    void HandlePieceFixed()
    {
        if (!bombNextPiece || isProcessing)
            return;

        StartCoroutine(BombSequence());
    }

    // =====================================================
    // ■ 爆弾メイン処理
    // =====================================================
    IEnumerator BombSequence()
    {
        isProcessing = true;
        bombNextPiece = false;

        // ① 時止め
        main.SetPause(true);
        if (agent != null)
            agent.isStopped = true;

        yield return null;

        // ② 爆発中心取得（最後に置かれたブロックを探す）
        Vector2Int center = FindLastPlacedBlock();

        // ③ 3×3爆破
        int destroyedByBomb = Explode3x3(center);

        // ④ ラインチェック（爆弾分は除外）
        int lineDestroyed = ClearLinesWithoutBuff();

        reservedDestroyed = lineDestroyed;

        yield return new WaitForSecondsRealtime(0.3f);

        // ⑤ 最大倍率適用
        ApplyReservedBuff();

        // ⑥ 再開
        main.SetPause(false);
        if (agent != null)
            agent.isStopped = false;

        // ⑦ デバフ
        StartCoroutine(StunPenalty());

        isProcessing = false;
    }

    // =====================================================
    // ■ 3×3爆発
    // =====================================================
    int Explode3x3(Vector2Int center)
    {
        int count = 0;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = center.x + dx;
                int y = center.y + dy;

                if (x >= 0 && x < Width &&
                    y >= 0 && y < Height)
                {
                    if (field[y, x] != 0)
                    {
                        field[y, x] = 0;
                        count++;
                    }
                }
            }
        }

        return count;
    }

    // =====================================================
    // ■ ライン削除（バフ計算のみ）
    // =====================================================
    int ClearLinesWithoutBuff()
    {
        int totalDestroyed = 0;

        for (int y = 0; y < Height; y++)
        {
            bool full = true;

            for (int x = 0; x < Width; x++)
            {
                if (field[y, x] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (!full) continue;

            for (int x = 0; x < Width; x++)
            {
                field[y, x] = 0;
                totalDestroyed++;
            }

            for (int yy = y; yy < Height - 1; yy++)
                for (int x = 0; x < Width; x++)
                    field[yy, x] = field[yy + 1, x];

            for (int x = 0; x < Width; x++)
                field[Height - 1, x] = 0;

            y--;
        }

        return totalDestroyed;
    }

    // =====================================================
    // ■ 最大倍率適用
    // =====================================================
    void ApplyReservedBuff()
    {
        if (reservedDestroyed <= 0)
            return;

        if (reservedDestroyed >= 54)
        {
            StartCoroutine(OverDrive());
            return;
        }

        float duration = reservedDestroyed * main.secondsPerBlock;
        float speedAmount = reservedDestroyed * main.speedPerBlock;

        main.StartCoroutine(ManualBuff(duration, speedAmount));
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
    // ■ デバフ
    // =====================================================
    IEnumerator StunPenalty()
    {
        if (agent == null) yield break;

        agent.isStopped = true;
        yield return new WaitForSeconds(2.5f);
        agent.isStopped = false;
    }

    // =====================================================
    // ■ 最後に置かれたブロック探索
    // =====================================================
    Vector2Int FindLastPlacedBlock()
    {
        for (int y = Height - 1; y >= 0; y--)
            for (int x = 0; x < Width; x++)
                if (field[y, x] != 0)
                    return new Vector2Int(x, y);

        return Vector2Int.zero;
    }
}