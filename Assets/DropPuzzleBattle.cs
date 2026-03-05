using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DropPuzzleBattle.cs 完全版
/// - 設計書ルール（家系図・引数経由・直接変数操作禁止）厳守
/// - Eキー爆弾予約リスト方式実装済み
/// - コメント残し
/// </summary>
public class DropPuzzleBattle : MonoBehaviour
{
    // --- ブロックPrefab ---
    public GameObject GridPrefub; // 1マスブロックのPrefab

    // --- フィールドサイズ ---
    private const int Width = 13;  // 横幅
    private const int Height = 22; // 高さ

    // --- フィールド状態保持 ---
    private int[,] field = new int[Height, Width];          // 各マスのブロックタイプ
    private GameObject[,] gridObjects;                     // 実際に表示するブロックオブジェクト

    // --- ピース管理 ---
    private const int PieceCount = 10;                     // ピース種類数
    private Dictionary<int, Vector2Int[]> pieceData;       // ピースの形状データ

    public event System.Action OnPieceFixed;              // ピース固定時に通知するイベント

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
        // --- グリッドオブジェクト生成 ---
        gridObjects = new GameObject[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                GameObject obj = Instantiate(GridPrefub);
                obj.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
                obj.SetActive(false);             // 最初は非表示
                gridObjects[y, x] = obj;          // 配列に格納
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
        for (int y = -1; y <= Height; y++)
        {
            CreateWallBlock(-1, y);           // 左壁
            CreateWallBlock(Width, y);        // 右壁
        }

        for (int x = -1; x <= Width; x++)
        {
            CreateWallBlock(x, -1);           // 下壁
            CreateWallBlock(x, Height);       // 上壁
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
        fallTimer += Time.deltaTime;                             // 落下タイマー加算
        float speed = Input.GetKey(KeyCode.S) ? 0.05f : fallInterval; // Sキー押下で高速落下

        if (fallTimer >= speed)
        {
            fallTimer = 0;
            Move(Vector2Int.down);                              // 下に移動
        }

        // 左右移動
        if (Input.GetKeyDown(KeyCode.A)) Move(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) Move(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) Rotate();             // 回転

        Draw();                                                 // 描画更新
    }

    // ==================================================
    // ピース形状初期化
    // ==================================================
    void InitPieces()
    {
        pieceData = new Dictionary<int, Vector2Int[]>();
        // 各ピースを座標配列で定義
        pieceData[0] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2) };
        pieceData[1] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        pieceData[2] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[3] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        pieceData[4] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) };
        pieceData[5] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        pieceData[6] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) };
        pieceData[7] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        pieceData[8] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        pieceData[9] = new[] { new Vector2Int(0, 0) };          // 1マスブロック
    }

    // ==================================================
    // 新しいピース生成
    // ==================================================
    void SpawnPiece()
    {
        pieceSpawnCount++;

        DropLogicExtension logic = FindObjectOfType<DropLogicExtension>(); // E爆弾予約取得用

        // 強制指定があれば優先
        if (forcedNextPieceType != -1)
        {
            currentType = forcedNextPieceType;
            forcedNextPieceType = -1;
        }
        else
        {
            // デフォルトピース生成
            int interval = pieceSpawnCount < 30 ? 10 : 7;
            int defaultType = (pieceSpawnCount % interval == 0) ? 8 : Random.Range(0, 8);

            if (logic != null)
                currentType = logic.GetNextPieceType(defaultType); // E爆弾予約考慮
            else
                currentType = defaultType;
        }

        currentShape = pieceData[currentType];
        currentPos = new Vector2Int(Width / 2, Height - 2); // 出現位置（上中央）

        // 出現できなければゲームオーバー
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
            currentPos = newPos; // 移動可能なら更新
        }
        else if (dir == Vector2Int.down)
        {
            // 下に移動できない場合 → ピース固定
            FixPiece();

            int destroyedBlocks = ClearLines();                  // ライン消去
            bool eKeyHit = ExplodeEKeyBombWithReservation();    // E爆弾処理（予約リスト方式）

            // E爆弾ヒット時に勇者デバフ適用
            if (eKeyHit && BattleMainManager.Instance != null)
            {
                BattleMainManager.Instance.ApplyEKeyDebuff(5f); // 5秒停止
            }

            DropLogicExtension logic = FindObjectOfType<DropLogicExtension>();
            if (logic != null)
                logic.OnEKeyBombFinished();                      // E爆弾処理完了通知

            if (!skipDestroyedNotification)
            {
                if (destroyedBlocks > 0 && BattleMainManager.Instance != null)
                    BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks); // ブロック破壊通知
            }

            SpawnPiece(); // 次ピース生成
        }
    }

    // ==================================================
    // Eキー爆弾処理（予約リスト方式）
    // ==================================================
    bool ExplodeEKeyBombWithReservation()
    {
        eKeyBombPositions.Clear();
        HashSet<int> verticalColumnsToExplode = new HashSet<int>();
        bool eKeyHit = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (field[y, x] == 10) // E爆弾を検出
                {
                    eKeyHit = true;

                    int xStart = Mathf.Max(0, x - 2);
                    int xEnd = Mathf.Min(Width - 1, x + 2);
                    int yStart = Mathf.Max(0, y - 2);
                    int yEnd = Mathf.Min(Height - 1, y + 2);

                    // 範囲内を予約
                    for (int yy = yStart; yy <= yEnd; yy++)
                        for (int xx = xStart; xx <= xEnd; xx++)
                        {
                            eKeyBombPositions.Add(new Vector2Int(xx, yy));
                            if (field[yy, xx] == 9) // 縦爆弾検出
                                verticalColumnsToExplode.Add(xx);
                        }

                    field[y, x] = 0; // 爆弾自身を消去
                }
            }
        }

        // 予約リストで範囲内消去
        foreach (var pos in eKeyBombPositions)
        {
            if (field[pos.y, pos.x] != 0)
                field[pos.y, pos.x] = 0;
        }

        // バーティカル列も上下全消去
        foreach (int col in verticalColumnsToExplode)
            for (int yy = 0; yy < Height; yy++)
                if (field[yy, col] != 0)
                    field[yy, col] = 0;

        return eKeyHit; // E爆弾がヒットしたか
    }

    // ==================================================
    // 回転処理
    // ==================================================
    void Rotate()
    {
        Vector2Int[] rotated = new Vector2Int[currentShape.Length];

        for (int i = 0; i < currentShape.Length; i++)
            rotated[i] = new Vector2Int(-currentShape[i].y, currentShape[i].x); // 90°回転

        if (IsValidPosition(currentPos, rotated))
            currentShape = rotated;
    }

    // ==================================================
    // 位置有効判定
    // ==================================================
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

    // ==================================================
    // ピース固定
    // ==================================================
    void FixPiece()
    {
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y >= 0 && p.y < Height)
                field[p.y, p.x] = currentType + 1; // 固定ブロックをフィールドに書き込み
        }

        OnPieceFixed?.Invoke(); // イベント通知
    }

    // ==================================================
    // ライン消去処理
    // ==================================================
    int ClearLines()
    {
        int totalDestroyed = 0;

        for (int y = 0; y < Height; y++)
        {
            bool full = true;

            for (int x = 0; x < Width; x++)
                if (field[y, x] == 0)
                {
                    full = false; // 空きあり
                    break;
                }

            if (!full) continue;

            HashSet<int> bombColumns = new HashSet<int>();

            for (int x = 0; x < Width; x++)
                if (field[y, x] == 9)
                    bombColumns.Add(x); // 縦爆弾列を記録

            // ライン消去
            for (int x = 0; x < Width; x++)
                if (field[y, x] != 0)
                {
                    field[y, x] = 0;
                    totalDestroyed++;
                }

            // 上の行を落とす
            for (int yy = y; yy < Height - 1; yy++)
                for (int x = 0; x < Width; x++)
                    field[yy, x] = field[yy + 1, x];

            // 最上行は空に
            for (int x = 0; x < Width; x++)
                field[Height - 1, x] = 0;

            // 縦爆弾列も上下全消去
            foreach (int col in bombColumns)
                for (int yy = 0; yy < Height; yy++)
                    if (field[yy, col] != 0)
                        field[yy, col] = 0;

            y--; // 次行も確認
        }

        return totalDestroyed;
    }

    // ==================================================
    // 描画処理
    // ==================================================
    void Draw()
    {
        // 一旦全て非表示
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                gridObjects[y, x].SetActive(false);

        // 固定ブロック描画
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (field[y, x] != 0)
                {
                    gridObjects[y, x].SetActive(true);
                    gridObjects[y, x].GetComponent<Renderer>().material.color = pieceColors[field[y, x] - 1];
                }

        // 落下中ブロック描画
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