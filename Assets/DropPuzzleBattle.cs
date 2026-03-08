using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// DropPuzzleBattle.cs 完全版
/// - 家系図ルール・引数経由・直接変数操作禁止
/// - Eキー爆弾5×5処理＋範囲外左右チェックによるバーティカル破壊
/// - コメント逐一
/// </summary>
public class DropPuzzleBattle : MonoBehaviour
{
    // --- ブロックPrefab ---
    public GameObject GridPrefub; // 1マスブロックのPrefab

    [SerializeField] private PuzzleFieldSO puzzleFieldSO;

    int hight => puzzleFieldSO.Hight;
    int wide => puzzleFieldSO.Wide;

    // --- フィールド状態保持 ---
    private int[,] field;          // 各マスのブロックタイプ
    private GameObject[,] gridObjects;                     // 実際に表示するブロックオブジェクト

    // --- ピース管理 ---
    private const int PieceCount = 10;                     // ピース種類数
    private Dictionary<int, Vector2Int[]> pieceData;       // ピース形状データ

    public event System.Action OnPieceFixed;              // ピース固定時のイベント

    private Vector2Int[] currentShape;                     // 落下中ピースの形状
    private Vector2Int currentPos;                         // 落下中ピースの位置
    private int currentType;                               // 落下中ピースの種類

    // --- 落下制御 ---
    private float fallTimer;                               // 落下タイマー
    private float fallInterval = 1f;                       // 自動落下間隔（秒）

    // --- 描画オフセット ---
    private float offsetX = 100f;                          // X座標オフセット
    private float offsetY = 0f;                            // Y座標オフセット

    // --- その他制御 ---
    private int pieceSpawnCount = 0;                       // 出現ピース数カウント
    private bool skipDestroyedNotification = false;        // ブロック破壊通知スキップフラグ（E爆弾用）
    private int forcedNextPieceType = -1;                 // 次ピース強制タイプ（-1は通常）

    // --- ピースカラー ---
    private Color[] pieceColors =
    {
        Color.cyan, Color.blue, Color.green, Color.red, Color.yellow,
        Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
        Color.black, Color.black
    };

    // --- Eキー爆弾予約リスト ---
    private List<Vector2Int> eKeyBombPositions = new List<Vector2Int>(); // 爆破対象位置リスト

    // ==================================================
    // Start: 初期化処理
    // ==================================================
    void Start()
    {
        field = new int[hight,wide]; //初期化処理

        gridObjects = new GameObject[hight, wide];
        for (int y = 0; y < hight; y++)
            for (int x = 0; x < wide; x++)
            {
                GameObject obj = Instantiate(GridPrefub);
                obj.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
                obj.SetActive(false);
                gridObjects[y, x] = obj;
            }

        CreateWall();   // 壁生成
        InitPieces();   // ピース初期化
        SpawnPiece();   // 最初のピース生成
    }

    // ==================================================
    // 壁生成
    // ==================================================
    void CreateWall()
    {
        for (int y = -1; y <= hight; y++)
        {
            CreateWallBlock(-1, y);           // 左壁
            CreateWallBlock(wide, y);        // 右壁
        }

        for (int x = -1; x <= wide; x++)
        {
            CreateWallBlock(x, -1);           // 下壁
            CreateWallBlock(x, hight);       // 上壁
        }
    }

    void CreateWallBlock(int x, int y)
    {
        GameObject wall = Instantiate(GridPrefub);
        wall.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
        wall.GetComponent<Renderer>().material.color = Color.gray; // 壁はグレー
    }

    // ==================================================
    // 毎フレーム更新
    // ==================================================
    void Update()
    {
        fallTimer += Time.deltaTime;
        float speed = Input.GetKey(KeyCode.S) ? 0.05f : fallInterval;

        if (fallTimer >= speed)
        {
            fallTimer = 0;
            Move(Vector2Int.down);  // 下に移動
        }

        if (Input.GetKeyDown(KeyCode.A)) Move(Vector2Int.left);   // 左
        if (Input.GetKeyDown(KeyCode.D)) Move(Vector2Int.right);  // 右
        if (Input.GetKeyDown(KeyCode.W)) Rotate();                // 回転

        Draw(); // 描画更新
    }

    // ==================================================
    // ピース形状初期化
    // ==================================================
    void InitPieces()
    {
        pieceData = new Dictionary<int, Vector2Int[]>();
        pieceData[0] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2) };
        pieceData[1] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        pieceData[2] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[3] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        pieceData[4] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) };
        pieceData[5] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        pieceData[6] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) };
        pieceData[7] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        pieceData[8] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        pieceData[9] = new[] { new Vector2Int(0, 0) };
    }

    // ==================================================
    // 新しいピース生成
    // ==================================================
    void SpawnPiece()
    {
        pieceSpawnCount++;
        DropLogicExtension logic = FindObjectOfType<DropLogicExtension>();

        if (forcedNextPieceType != -1)
        {
            currentType = forcedNextPieceType;
            forcedNextPieceType = -1;
        }
        else
        {
            int interval = pieceSpawnCount < 30 ? 10 : 7;
            int defaultType = (pieceSpawnCount % interval == 0) ? 8 : Random.Range(0, 8);
            if (logic != null)
                currentType = logic.GetNextPieceType(defaultType);
            else
                currentType = defaultType;
        }

        currentShape = pieceData[currentType];
        currentPos = new Vector2Int(wide / 2, hight - 2);

        if (!IsValidPosition(currentPos, currentShape))
        {
            Debug.Log("Game Over");
            enabled = false;
        }
    }

    // ==================================================
    // ピース移動処理
    // ==================================================
    void Move(Vector2Int dir)
    {
        Vector2Int newPos = currentPos + dir;

        if (IsValidPosition(newPos, currentShape))
        {
            currentPos = newPos;
        }
        else if (dir == Vector2Int.down)
        {
            FixPiece();
            int destroyedBlocks = ClearLines();
            bool eKeyHit = ExplodeEKeyBombWithReservation(); // 新ロジック

            if (eKeyHit && BattleMainManager.Instance != null)
                BattleMainManager.Instance.ApplyEKeyDebuff(5f);

            DropLogicExtension logic = FindObjectOfType<DropLogicExtension>();
            if (logic != null)
                logic.OnEKeyBombFinished();

            if (!skipDestroyedNotification && destroyedBlocks > 0 && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);

            SpawnPiece();
        }
    }

    // ==================================================
    // E爆弾処理（5×5 + 隣マスチェックによる縦爆弾消去）
    // ==================================================
    bool ExplodeEKeyBombWithReservation()
    {
        eKeyBombPositions.Clear();
        HashSet<int> verticalColumnsToExplode = new HashSet<int>();
        bool eKeyHit = false;

        // フィールド全体を探索
        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                if (field[y, x] == 10) // E爆弾
                {
                    eKeyHit = true;

                    // 5x5爆破範囲
                    int xStart = Mathf.Max(0, x - 2);
                    int xEnd = Mathf.Min(wide - 1, x + 2);
                    int yStart = Mathf.Max(0, y - 2);
                    int yEnd = Mathf.Min(hight - 1, y + 2);

                    // まず範囲内を予約
                    for (int yy = yStart; yy <= yEnd; yy++)
                        for (int xx = xStart; xx <= xEnd; xx++)
                        {
                            eKeyBombPositions.Add(new Vector2Int(xx, yy));

                            // 縦爆弾確認
                            if (field[yy, xx] == 9)
                            {
                                // 左右隣チェック（範囲外も含む）
                                bool leftBlack = (xx - 1 >= 0 && field[yy, xx - 1] == 9);
                                bool rightBlack = (xx + 1 < wide && field[yy, xx + 1] == 9);

                                if (leftBlack) verticalColumnsToExplode.Add(xx - 1); // 左隣列も縦消去
                                if (rightBlack) verticalColumnsToExplode.Add(xx + 1); // 右隣列も縦消去
                                verticalColumnsToExplode.Add(xx);                     // 自身の列縦消去
                            }
                        }

                    field[y, x] = 0; // 爆弾自身消去
                }
            }
        }

        // 予約リスト消去
        foreach (var pos in eKeyBombPositions)
            if (field[pos.y, pos.x] != 0)
                field[pos.y, pos.x] = 0;

        // 縦列消去
        foreach (int col in verticalColumnsToExplode)
            for (int yy = 0; yy < hight; yy++)
                if (field[yy, col] != 0)
                    field[yy, col] = 0;

        return eKeyHit;
    }

    // ==================================================
    // 回転
    // ==================================================
    void Rotate()
    {
        Vector2Int[] rotated = new Vector2Int[currentShape.Length];
        for (int i = 0; i < currentShape.Length; i++)
            rotated[i] = new Vector2Int(-currentShape[i].y, currentShape[i].x);

        if (IsValidPosition(currentPos, rotated))
            currentShape = rotated;
    }

    // ==================================================
    // 有効位置判定
    // ==================================================
    bool IsValidPosition(Vector2Int pos, Vector2Int[] shape)
    {
        foreach (var block in shape)
        {
            Vector2Int p = pos + block;
            if (p.x < 0 || p.x >= wide) return false;
            if (p.y < 0) return false;
            if (p.y < hight && field[p.y, p.x] != 0) return false;
        }
        return true;
    }

    // ==================================================
    // ピース固定
    // ==================================================
    void FixPiece()
    {
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y >= 0 && p.y < hight)
                field[p.y, p.x] = currentType + 1;
        }

        OnPieceFixed?.Invoke();
    }

    // ==================================================
    // ライン消去
    // ==================================================
    int ClearLines()
    {
        int totalDestroyed = 0;

        for (int y = 0; y < hight; y++)
        {
            bool full = true;
            for (int x = 0; x < wide; x++)
                if (field[y, x] == 0) { full = false; break; }
            if (!full) continue;

            HashSet<int> bombColumns = new HashSet<int>();
            for (int x = 0; x < wide; x++)
                if (field[y, x] == 9) bombColumns.Add(x);

            for (int x = 0; x < wide; x++)
                if (field[y, x] != 0) { field[y, x] = 0; totalDestroyed++; }

            for (int yy = y; yy < hight - 1; yy++)
                for (int x = 0; x < wide; x++)
                    field[yy, x] = field[yy + 1, x];

            for (int x = 0; x < wide; x++)
                field[hight - 1, x] = 0;

            foreach (int col in bombColumns)
                for (int yy = 0; yy < hight; yy++)
                    if (field[yy, col] != 0) field[yy, col] = 0;

            y--;
        }

        return totalDestroyed;
    }

    // ==================================================
    // 描画
    // ==================================================
    void Draw()
    {
        for (int y = 0; y < hight; y++)
            for (int x = 0; x < wide; x++)
                gridObjects[y, x].SetActive(false);

        for (int y = 0; y < hight; y++)
            for (int x = 0; x < wide; x++)
                if (field[y, x] != 0)
                {
                    gridObjects[y, x].SetActive(true);
                    gridObjects[y, x].GetComponent<Renderer>().material.color = pieceColors[field[y, x] - 1];
                }

        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y >= 0 && p.y < hight)
            {
                gridObjects[p.y, p.x].SetActive(true);
                gridObjects[p.y, p.x].GetComponent<Renderer>().material.color = pieceColors[currentType];
            }
        }
    }

    // ==================================================
    // 通知スキップ設定
    // ==================================================
    public void SetSkipDestroyedNotification(bool value)
    {
        skipDestroyedNotification = value;
    }

    // ==================================================
    // 次ピース強制設定
    // ==================================================
    public void ForceNextPieceType(int type)
    {
        forcedNextPieceType = type;
    }
}