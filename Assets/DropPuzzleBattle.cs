using System.Collections.Generic;
using UnityEngine;

public class DropPuzzleBattle : MonoBehaviour
{
    // Prefab for individual grid blocks
    public GameObject GridPrefub;

    // フィールドの横幅と高さ
    private const int Width = 13;
    private const int Height = 22;

    // ゲーム盤のデータ（ブロックタイプを保持）
    private int[,] field = new int[Height, Width];

    // 実際のGridオブジェクト参照
    private GameObject[,] gridObjects;

    // ピース種類数
    private const int PieceCount = 10;

    // ピースごとの形状データ
    private Dictionary<int, Vector2Int[]> pieceData;

    // ピース固定時に通知するイベント
    public event System.Action OnPieceFixed;

    // 現在落下中のピース情報
    private Vector2Int[] currentShape;
    private Vector2Int currentPos;
    private int currentType;

    // 落下タイマーと間隔
    private float fallTimer;
    private float fallInterval = 1f;

    // 描画オフセット（画面上の位置調整用）
    private float offsetX = 100f;
    private float offsetY = 0f;

    // スポーンされたピースのカウント
    private int pieceSpawnCount = 0;

    // ブロック破壊通知スキップ用フラグ
    private bool skipDestroyedNotification = false;

    // 次のピースを強制指定するための変数
    private int forcedNextPieceType = -1;

    // ピースの色配列
    private Color[] pieceColors =
    {
        Color.cyan,
        Color.blue,
        Color.green,
        Color.red,
        Color.yellow,
        Color.magenta,
        new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 1f),
        Color.black,
        Color.black
    };

    void Start()
    {
        // グリッドオブジェクトを生成して非表示に
        gridObjects = new GameObject[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                GameObject obj = Instantiate(GridPrefub);
                obj.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
                obj.SetActive(false);
                gridObjects[y, x] = obj;
            }

        // 壁を生成
        CreateWall();

        // ピース形状を初期化
        InitPieces();

        // 最初のピースを生成
        SpawnPiece();
    }

    // フィールド外枠の壁を生成
    void CreateWall()
    {
        for (int y = -1; y <= Height; y++)
        {
            CreateWallBlock(-1, y);     // 左壁
            CreateWallBlock(Width, y);  // 右壁
        }

        for (int x = -1; x <= Width; x++)
        {
            CreateWallBlock(x, -1);     // 下壁
            CreateWallBlock(x, Height); // 上壁
        }
    }

    void CreateWallBlock(int x, int y)
    {
        GameObject wall = Instantiate(GridPrefub);
        wall.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
        wall.GetComponent<Renderer>().material.color = Color.gray; // 壁色
    }

    void Update()
    {
        // 自動落下処理
        fallTimer += Time.deltaTime;
        float speed = Input.GetKey(KeyCode.S) ? 0.05f : fallInterval; // Sで早落ち

        if (fallTimer >= speed)
        {
            fallTimer = 0;
            Move(Vector2Int.down);
        }

        // 左右移動、回転
        if (Input.GetKeyDown(KeyCode.A)) Move(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) Move(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) Rotate();

        // 描画更新
        Draw();
    }

    // ピース形状を初期化
    void InitPieces()
    {
        pieceData = new Dictionary<int, Vector2Int[]>();

        // 各ピース形状を格納（座標は相対座標）
        pieceData[0] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2) };
        pieceData[1] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        pieceData[2] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[3] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        pieceData[4] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) };
        pieceData[5] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        pieceData[6] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) };
        pieceData[7] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };

        // 四角2x2（定期出現用）
        pieceData[8] = new[]
        {
            new Vector2Int(0,0),
            new Vector2Int(1,0),
            new Vector2Int(0,1),
            new Vector2Int(1,1)
        };

        // Eキー爆弾
        pieceData[9] = new[]
        {
            new Vector2Int(0,0)
        };
    }

    // 新しいピースを生成
    void SpawnPiece()
    {
        pieceSpawnCount++;

        DropLogicExtension logic = FindObjectOfType<DropLogicExtension>();

        if (forcedNextPieceType != -1)
        {
            // 強制タイプがあれば使用
            currentType = forcedNextPieceType;
            forcedNextPieceType = -1;
        }
        else
        {
            // 定期的に四角ブロック出現
            int interval = pieceSpawnCount < 30 ? 10 : 7;
            int defaultType = (pieceSpawnCount % interval == 0) ? 8 : Random.Range(0, 8);

            // DropLogicExtension でE爆弾予約があれば反映
            if (logic != null)
                currentType = logic.GetNextPieceType(defaultType);
            else
                currentType = defaultType;
        }

        currentShape = pieceData[currentType];
        currentPos = new Vector2Int(Width / 2, Height - 2);

        // 配置不可ならゲームオーバー
        if (!IsValidPosition(currentPos, currentShape))
        {
            Debug.Log("Game Over");
            enabled = false;
        }
    }

    // ピース移動
    void Move(Vector2Int dir)
    {
        Vector2Int newPos = currentPos + dir;

        if (IsValidPosition(newPos, currentShape))
        {
            currentPos = newPos; // 移動可能なら更新
        }
        else if (dir == Vector2Int.down)
        {
            // 底に着いた場合、固定処理
            FixPiece();

            // ライン消去
            int destroyedBlocks = ClearLines();

            // Eキー爆弾の処理
            bool eKeyHit = ExplodeEKeyBomb();

            // 着弾なら勇者デバフ
            if (eKeyHit && BattleMainManager.Instance != null)
            {
                BattleMainManager.Instance.ApplyEKeyDebuff(5f);
            }

            // DropLogicExtension に爆弾終了通知
            DropLogicExtension logic = FindObjectOfType<DropLogicExtension>();
            if (logic != null)
                logic.OnEKeyBombFinished();

            // 通常の破壊通知
            if (!skipDestroyedNotification)
            {
                if (destroyedBlocks > 0 && BattleMainManager.Instance != null)
                    BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);
            }

            // 新しいピース生成
            SpawnPiece();
        }
    }

    // Eキー爆弾着弾判定＆爆破
    bool ExplodeEKeyBomb()
    {
        HashSet<int> verticalColumnsToExplode = new HashSet<int>();
        bool eKeyHit = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (field[y, x] == 10) // E爆弾
                {
                    eKeyHit = true;

                    int xStart = Mathf.Max(0, x - 2);
                    int xEnd = Mathf.Min(Width - 1, x + 2);
                    int yStart = Mathf.Max(0, y - 2);
                    int yEnd = Mathf.Min(Height - 1, y + 2);

                    // 5x5範囲のブロック消去
                    for (int yy = yStart; yy <= yEnd; yy++)
                        for (int xx = xStart; xx <= xEnd; xx++)
                        {
                            if (field[yy, xx] != 9) // 通常ブロックを消す
                                field[yy, xx] = 0;

                            if (field[yy, xx] == 9) // バーティカル爆弾列保存
                                verticalColumnsToExplode.Add(xx);
                        }

                    field[y, x] = 0; // 自身のE爆弾消去
                }
            }
        }

        // バーティカル爆弾列を上下全消去
        foreach (int col in verticalColumnsToExplode)
            for (int yy = 0; yy < Height; yy++)
                if (field[yy, col] != 0)
                    field[yy, col] = 0;

        return eKeyHit;
    }

    // ピース回転
    void Rotate()
    {
        Vector2Int[] rotated = new Vector2Int[currentShape.Length];

        for (int i = 0; i < currentShape.Length; i++)
            rotated[i] = new Vector2Int(-currentShape[i].y, currentShape[i].x);

        if (IsValidPosition(currentPos, rotated))
            currentShape = rotated;
    }

    // 配置可能判定
    bool IsValidPosition(Vector2Int pos, Vector2Int[] shape)
    {
        foreach (var block in shape)
        {
            Vector2Int p = pos + block;
            if (p.x < 0 || p.x >= Width) return false;
            if (p.y < 0) return false;
            if (p.y < Height && field[p.y, p.x] != 0) return false;
        }
        return true;
    }

    // ピース固定
    void FixPiece()
    {
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y >= 0 && p.y < Height)
                field[p.y, p.x] = currentType + 1;
        }

        // 固定時イベント通知
        OnPieceFixed?.Invoke();
    }

    // ライン消去処理
    int ClearLines()
    {
        int totalDestroyed = 0;

        for (int y = 0; y < Height; y++)
        {
            bool full = true;

            for (int x = 0; x < Width; x++)
                if (field[y, x] == 0)
                {
                    full = false;
                    break;
                }

            if (!full) continue;

            HashSet<int> bombColumns = new HashSet<int>();

            // ライン上のバーティカル爆弾記録
            for (int x = 0; x < Width; x++)
                if (field[y, x] == 9)
                    bombColumns.Add(x);

            // ライン消去
            for (int x = 0; x < Width; x++)
                if (field[y, x] != 0)
                {
                    field[y, x] = 0;
                    totalDestroyed++;
                }

            // 上の行を1段落下
            for (int yy = y; yy < Height - 1; yy++)
                for (int x = 0; x < Width; x++)
                    field[yy, x] = field[yy + 1, x];

            // 最上段を空に
            for (int x = 0; x < Width; x++)
                field[Height - 1, x] = 0;

            // バーティカル爆弾列を上下全消去
            foreach (int col in bombColumns)
                for (int yy = 0; yy < Height; yy++)
                    if (field[yy, col] != 0)
                        field[yy, col] = 0;

            y--;
        }

        return totalDestroyed;
    }

    // 描画更新
    void Draw()
    {
        // 一旦全グリッド非表示
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                gridObjects[y, x].SetActive(false);

        // 固定ブロックを描画
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (field[y, x] != 0)
                {
                    gridObjects[y, x].SetActive(true);
                    gridObjects[y, x].GetComponent<Renderer>().material.color = pieceColors[field[y, x] - 1];
                }

        // 落下中のブロック描画
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;

            if (p.y >= 0 && p.y < Height)
            {
                gridObjects[p.y, p.x].SetActive(true);
                gridObjects[p.y, p.x].GetComponent<Renderer>().material.color = pieceColors[currentType];
            }
        }
    }

    // 破壊通知スキップ設定
    public void SetSkipDestroyedNotification(bool value)
    {
        skipDestroyedNotification = value;
    }

    // 次のピースタイプを強制
    public void ForceNextPieceType(int type)
    {
        forcedNextPieceType = type;
    }
}