using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DropPuzzleBattle.cs
/// - BlockType enumによるブロック種別管理
/// - 爆弾処理: グリッド単位の繰り返し評価による連鎖方式
///   → 1グリッドずつ爆弾を評価・消去し、変化がなくなるまでループ
/// - FindObjectOfType・GetComponentキャッシュ化
/// </summary>
public class DropPuzzleBattle : MonoBehaviour
{
    // ==================================================
    // BlockType enum: フィールド上のブロック種別
    // ==================================================
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
        VerticalBomb = 9,  // 縦十字爆弾
        EKeyBomb = 10, // Eキー爆弾（5×5範囲）
    }

    // --- Prefab ---
    public GameObject GridPrefub;

    [SerializeField] private PuzzleFieldSO puzzleFieldSO;

    int hight => puzzleFieldSO.Hight;
    int wide => puzzleFieldSO.Wide;

    private GumiData gumiData = new();

    // --- フィールド状態保持 ---
    private BlockType[,] field;
    private GameObject[,] gridObjects;
    private Renderer[,] gridRenderers;

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

    private Color[] pieceColors => gumiData.pieceColors;

    // --- キャッシュ ---
    private DropLogicExtension cachedLogic;

    // ==================================================
    // Start
    // ==================================================
    void Start()
    {
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
                gridRenderers[y, x] = obj.GetComponent<Renderer>();
            }
        }

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
    // Update
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
    // ピース生成
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
    // 移動
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
            bool bombHit = ExplodeBombs();
            ApplyGravity(); // 爆発・消去後の浮きブロックを落下させる

            if (bombHit && BattleMainManager.Instance != null)
                BattleMainManager.Instance.ApplyEKeyDebuff(5f);

            cachedLogic?.OnEKeyBombFinished();

            if (!skipDestroyedNotification && destroyedBlocks > 0 && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);

            SpawnPiece();
        }
    }

    // ==================================================
    // 行シフト処理
    //
    // 空行（y行のx=0〜wide-1が全てEmpty）を見つけたら、
    // その行より上の全ブロックを1行分下にずらす。
    // これをhight回繰り返すことで複数の空行も解消される。
    // ==================================================
    void ApplyGravity()
    {
        for (int repeat = 0; repeat < hight; repeat++)
        {
            for (int y = 0; y < hight - 1; y++)
            {
                // y行が空行か確認
                bool isEmpty = true;
                for (int x = 0; x < wide; x++)
                    if (field[y, x] != BlockType.Empty) { isEmpty = false; break; }

                if (!isEmpty) continue;

                // 空行より上を1行分下にずらす
                for (int yy = y; yy < hight - 1; yy++)
                    for (int x = 0; x < wide; x++)
                        field[yy, x] = field[yy + 1, x];

                // 一番上の行を空にする
                for (int x = 0; x < wide; x++)
                    field[hight - 1, x] = BlockType.Empty;
            }
        }
    }

    // ==================================================
    // EKeyBomb処理（グリッド単位の繰り返し評価による連鎖方式）
    //
    // 【連鎖フロー】
    //   ① フィールド全体を1マスずつ走査
    //   ② EKeyBombを発見 → DestroyCell() で5×5範囲を1マスずつ評価しながら消去
    //   ③ 変化がなくなるまで繰り返す
    // ==================================================
    bool ExplodeBombs()
    {
        bool anyBombHit = false;

        bool changed = true;
        while (changed)
        {
            changed = false;

            for (int y = 0; y < hight; y++)
            {
                for (int x = 0; x < wide; x++)
                {
                    if (field[y, x] != BlockType.EKeyBomb) continue;

                    field[y, x] = BlockType.Empty; // 自身を先に消去（再トリガー防止）

                    int xStart = Mathf.Max(0, x - 2);
                    int xEnd = Mathf.Min(wide - 1, x + 2);
                    int yStart = Mathf.Max(0, y - 2);
                    int yEnd = Mathf.Min(hight - 1, y + 2);

                    // 5×5範囲を1マスずつ評価して消去
                    for (int yy = yStart; yy <= yEnd; yy++)
                        for (int xx = xStart; xx <= xEnd; xx++)
                            DestroyCell(xx, yy);

                    changed = true;
                    anyBombHit = true;
                }
            }
        }

        return anyBombHit;
    }

    // ==================================================
    // 1マス評価して消去
    // 爆弾であれば先に起爆してから消去することで連鎖を実現する
    //
    //   - EKeyBomb  → ExplodeBombs() のループが次パスで再起爆するため何もしない
    //                 （自身消去のみ。既にEmptyでないなら次ループで拾われる）
    //   - VerticalBomb → 十字範囲を1マスずつ再帰的に DestroyCell() で評価
    //   - 通常ブロック  → そのまま消去
    // ==================================================
    void DestroyCell(int x, int y)
    {
        if (field[y, x] == BlockType.Empty) return; // 空マスは何もしない

        if (field[y, x] == BlockType.VerticalBomb)
        {
            field[y, x] = BlockType.Empty; // 自身を先に消去（再トリガー防止）

            // 縦列を1マスずつ評価
            for (int yy = 0; yy < hight; yy++)
                DestroyCell(x, yy);

            // 横行を1マスずつ評価
            for (int xx = 0; xx < wide; xx++)
                DestroyCell(xx, y);
        }
        else
        {
            // 通常ブロック・EKeyBomb（次ループで起爆）はそのまま消去
            field[y, x] = BlockType.Empty;
        }
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
            bool full = true;
            for (int x = 0; x < wide; x++)
                if (field[y, x] == BlockType.Empty) { full = false; break; }
            if (!full) continue;

            // VerticalBombの位置を消去前に記録（列と行の両方）
            // 行はシフト後にずれるため、消去前のy座標を保持する
            List<Vector2Int> verticalBombs = new List<Vector2Int>();
            for (int x = 0; x < wide; x++)
                if (field[y, x] == BlockType.VerticalBomb) verticalBombs.Add(new Vector2Int(x, y));

            // ライン消去
            for (int x = 0; x < wide; x++)
                if (field[y, x] != BlockType.Empty) { field[y, x] = BlockType.Empty; totalDestroyed++; }

            // 上のラインを1段下にシフト
            for (int yy = y; yy < hight - 1; yy++)
                for (int x = 0; x < wide; x++)
                    field[yy, x] = field[yy + 1, x];

            for (int x = 0; x < wide; x++)
                field[hight - 1, x] = BlockType.Empty;

            // VerticalBombの受動爆発
            // 消去前のy座標を使って十字を1マスずつ評価しながら消去
            foreach (var bomb in verticalBombs)
            {
                // 縦列を1マスずつ評価
                for (int yy = 0; yy < hight; yy++)
                    DestroyCell(bomb.x, yy);

                // 横行を1マスずつ評価（消去前の行位置が基準）
                for (int xx = 0; xx < wide; xx++)
                    DestroyCell(xx, bomb.y);
            }

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
                    gridRenderers[y, x].material.color = pieceColors[(int)field[y, x] - 1];
                }
            }
        }

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
