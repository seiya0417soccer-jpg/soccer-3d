using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DropPuzzleBattle.cs
/// - BlockType enumによるブロック種別管理
/// - FindObjectOfType・GetComponentキャッシュ化
/// </summary>
public class DropPuzzleBattle : MonoBehaviour
{
    // ==================================================
    // BlockType enum: フィールド上のブロック種別
    // ==================================================
    /// <summary>
    /// フィールド各マスのブロック種別
    /// </summary>
    public enum BlockType
    {
        Empty = 0,  // 空マス
        Piece1 = 1,  // 通常ピース1
        Piece2 = 2,  // 通常ピース2
        Piece3 = 3,  // 通常ピース3
        Piece4 = 4,  // 通常ピース4
        Piece5 = 5,  // 通常ピース5
        Piece6 = 6,  // 通常ピース6
        Piece7 = 7,  // 通常ピース7
        Piece8 = 8,  // 通常ピース8
        VerticalBomb = 9, // 縦爆弾（列全体を消去）
        EKeyBomb = 10, // Eキー爆弾（5×5範囲爆破）
    }

    // --- ブロックPrefab ---
    public GameObject GridPrefub;

    [SerializeField] private PuzzleFieldSO puzzleFieldSO;

    int hight => puzzleFieldSO.Hight;
    int wide => puzzleFieldSO.Wide;

    private GumiData gumiData = new();

    // --- フィールド状態保持 ---
    /// <summary>各マスのブロック種別（BlockType enumで管理）</summary>
    private BlockType[,] field;
    private GameObject[,] gridObjects;
    private Renderer[,] gridRenderers; // Rendererキャッシュ

    private Dictionary<int, Vector2Int[]> pieceData => gumiData.pieceData;

    public event System.Action OnPieceFixed;

    private Vector2Int[] currentShape;
    private Vector2Int currentPos;
    private int currentType;

    // --- 落下制御 ---
    private float fallTimer;
    private float fallInterval = 1f;

    // --- 描画オフセット ---
    private float offsetX = 100f;
    private float offsetY = 0f;

    // --- その他制御 ---
    private int pieceSpawnCount = 0;
    private bool skipDestroyedNotification = false;
    private int forcedNextPieceType = -1;

    // --- ピースカラー ---
    private Color[] pieceColors => gumiData.pieceColors;

    // --- Eキー爆弾予約リスト ---
    private List<Vector2Int> eKeyBombPositions = new List<Vector2Int>();

    // --- キャッシュ ---
    private DropLogicExtension cachedLogic; // FindObjectOfTypeキャッシュ

    // ==================================================
    // Start: 初期化
    // ==================================================
    void Start()
    {
        // BlockType enumの配列として初期化（デフォルト値はEmpty=0）
        field = new BlockType[hight, wide];
        gridObjects = new GameObject[hight, wide];
        gridRenderers = new Renderer[hight, wide];

        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                GameObject obj = Instantiate(GridPrefub);
                obj.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
                obj.SetActive(false);
                gridObjects[y, x] = obj;
                gridRenderers[y, x] = obj.GetComponent<Renderer>(); // キャッシュ
            }
        }

        // DropLogicExtensionをキャッシュ
        cachedLogic = FindObjectOfType<DropLogicExtension>();

        CreateWall();
        SpawnPiece();
    }

    // ==================================================
    // 壁生成
    // ==================================================
    void CreateWall()
    {
        for (int y = -1; y <= hight; y++)
        {
            CreateWallBlock(-1, y);
            CreateWallBlock(wide, y);
        }
        for (int x = -1; x <= wide; x++)
        {
            CreateWallBlock(x, -1);
            CreateWallBlock(x, hight);
        }
    }

    void CreateWallBlock(int x, int y)
    {
        GameObject wall = Instantiate(GridPrefub);
        wall.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
        wall.GetComponent<Renderer>().material.color = Color.gray;
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
            Move(Vector2Int.down);
        }

        if (Input.GetKeyDown(KeyCode.A)) Move(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) Move(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) Rotate();

        Draw();
    }

    // ==================================================
    // 新しいピース生成
    // ==================================================
    void SpawnPiece()
    {
        pieceSpawnCount++;

        if (forcedNextPieceType != -1)
        {
            currentType = forcedNextPieceType;
            forcedNextPieceType = -1;
        }
        else
        {
            int interval = pieceSpawnCount < 30 ? 10 : 7;
            int defaultType = (pieceSpawnCount % interval == 0) ? 8 : Random.Range(0, 8);
            currentType = cachedLogic != null
                ? cachedLogic.GetNextPieceType(defaultType)
                : defaultType;
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
    // ピース移動
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
            bool eKeyHit = ExplodeEKeyBombWithReservation();

            if (eKeyHit && BattleMainManager.Instance != null)
                BattleMainManager.Instance.ApplyEKeyDebuff(5f);

            cachedLogic?.OnEKeyBombFinished();

            if (!skipDestroyedNotification && destroyedBlocks > 0 && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);

            SpawnPiece();
        }
    }

    // ==================================================
    // E爆弾処理（5×5 + 縦爆弾チェック）
    // ==================================================
    bool ExplodeEKeyBombWithReservation()
    {
        eKeyBombPositions.Clear();
        HashSet<int> verticalColumnsToExplode = new HashSet<int>();
        bool eKeyHit = false;

        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                if (field[y, x] != BlockType.EKeyBomb) continue;

                eKeyHit = true;

                int xStart = Mathf.Max(0, x - 2);
                int xEnd = Mathf.Min(wide - 1, x + 2);
                int yStart = Mathf.Max(0, y - 2);
                int yEnd = Mathf.Min(hight - 1, y + 2);

                for (int yy = yStart; yy <= yEnd; yy++)
                {
                    for (int xx = xStart; xx <= xEnd; xx++)
                    {
                        eKeyBombPositions.Add(new Vector2Int(xx, yy));

                        // 縦爆弾チェック
                        if (field[yy, xx] == BlockType.VerticalBomb)
                        {
                            bool leftVertical = (xx - 1 >= 0 && field[yy, xx - 1] == BlockType.VerticalBomb);
                            bool rightVertical = (xx + 1 < wide && field[yy, xx + 1] == BlockType.VerticalBomb);

                            verticalColumnsToExplode.Add(xx);
                            if (leftVertical) verticalColumnsToExplode.Add(xx - 1);
                            if (rightVertical) verticalColumnsToExplode.Add(xx + 1);
                        }
                    }
                }

                field[y, x] = BlockType.Empty; // 爆弾自身を消去
            }
        }

        // 予約リスト消去
        foreach (var pos in eKeyBombPositions)
            if (field[pos.y, pos.x] != BlockType.Empty)
                field[pos.y, pos.x] = BlockType.Empty;

        // 縦列消去
        foreach (int col in verticalColumnsToExplode)
            for (int yy = 0; yy < hight; yy++)
                field[yy, col] = BlockType.Empty;

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
            if (p.y < hight && field[p.y, p.x] != BlockType.Empty) return false;
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
                // currentTypeは0始まりなので+1してBlockTypeにキャスト
                field[p.y, p.x] = (BlockType)(currentType + 1);
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
            // ラインが埋まっているか確認
            bool full = true;
            for (int x = 0; x < wide; x++)
                if (field[y, x] == BlockType.Empty) { full = false; break; }
            if (!full) continue;

            // 縦爆弾列を記録
            HashSet<int> bombColumns = new HashSet<int>();
            for (int x = 0; x < wide; x++)
                if (field[y, x] == BlockType.VerticalBomb) bombColumns.Add(x);

            // ライン消去
            for (int x = 0; x < wide; x++)
                if (field[y, x] != BlockType.Empty) { field[y, x] = BlockType.Empty; totalDestroyed++; }

            // 上のラインを1段下にシフト
            for (int yy = y; yy < hight - 1; yy++)
                for (int x = 0; x < wide; x++)
                    field[yy, x] = field[yy + 1, x];

            for (int x = 0; x < wide; x++)
                field[hight - 1, x] = BlockType.Empty;

            // 縦爆弾列を全消去
            foreach (int col in bombColumns)
                for (int yy = 0; yy < hight; yy++)
                    field[yy, col] = BlockType.Empty;

            y--;
        }

        return totalDestroyed;
    }

    // ==================================================
    // 描画
    // ==================================================
    void Draw()
    {
        // フィールドブロック描画
        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                if (field[y, x] == BlockType.Empty)
                {
                    gridObjects[y, x].SetActive(false);
                }
                else
                {
                    gridObjects[y, x].SetActive(true);
                    // BlockType(1〜)→colorIndex(0〜)に変換
                    int colorIndex = (int)field[y, x] - 1;
                    gridRenderers[y, x].material.color = pieceColors[colorIndex];
                }
            }
        }

        // 落下中ピース描画
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y >= 0 && p.y < hight)
            {
                gridObjects[p.y, p.x].SetActive(true);
                gridRenderers[p.y, p.x].material.color = pieceColors[currentType];
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