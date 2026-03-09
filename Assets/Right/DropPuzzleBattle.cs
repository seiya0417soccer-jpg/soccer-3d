using System.Collections;
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
        Empty = 0,
        Piece1 = 1,
        Piece2 = 2,
        Piece3 = 3,
        Piece4 = 4,
        Piece5 = 5,
        Piece6 = 6,
        Piece7 = 7,
        Piece8 = 8,
        Piece9 = 9,  // 白・旧CrossBomb形状
        CrossBomb = 10, // 十字爆弾（1マス・黒）
        EKeyBomb = 11, // Eキー爆弾（5×5範囲）
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
    private bool skipDestroyedNotification = false;
    private int forcedNextPieceType = -1;

    [SerializeField, Range(0f, 1f)] private float crossBombChance = 0.1f;

    private Color[] pieceColors => gumiData.pieceColors;

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

    private HashSet<int> crossBombBlockIndices = new HashSet<int>();

    // ==================================================
    // ピース生成
    // ==================================================
    void SpawnPiece()
    {
        crossBombBlockIndices.Clear();

        if (forcedNextPieceType != -1)
        {
            currentType = forcedNextPieceType;
            forcedNextPieceType = -1;
        }
        else
        {
            int defaultType = Random.Range(0, 9);
            currentType = cachedLogic != null
                ? cachedLogic.GetNextPieceType(defaultType)
                : defaultType;
        }

        currentShape = pieceData[currentType];

        // EKeyBombはCrossBombマークをスキップ
        if (currentType != (int)BlockType.EKeyBomb)
        {
            for (int i = 0; i < currentShape.Length; i++)
                if (Random.value < crossBombChance)
                    crossBombBlockIndices.Add(i);
        }

        currentPos = new Vector2Int(wide / 2, hight - 2);

        if (!IsValidPosition(currentPos, currentShape))
        {
            Debug.Log("Game Over");
            enabled = false;
            GameFlowManager.Instance?.OnGameOver();
        }
    }

    private bool isProcessingIslands = false;

    // ==================================================
    // 移動
    // ==================================================
    void Move(Vector2Int dir)
    {
        if (isProcessingIslands) return;

        Vector2Int newPos = currentPos + dir;

        if (IsValidPosition(newPos, currentShape))
        {
            currentPos = newPos;
        }
        else if (dir == Vector2Int.down)
        {
            FixPiece();
            int destroyedBlocks = ClearLines();
            int bombDestroyed = ExplodeBombs();
            bool eKeyHit = bombDestroyed > 0;
            destroyedBlocks += bombDestroyed;

            if (eKeyHit && BattleMainManager.Instance != null)
                BattleMainManager.Instance.ApplyEKeyDebuff(5f);

            cachedLogic?.OnEKeyBombFinished();

            if (!skipDestroyedNotification && destroyedBlocks > 0 && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);

            StartCoroutine(ProcessIslandsThenSpawn());
        }
    }

    // ==================================================
    // 浮き島処理 → ライン消去連鎖 → SpawnPiece
    // ==================================================
    IEnumerator ProcessIslandsThenSpawn()
    {
        isProcessingIslands = true;

        while (true)
        {
            List<List<Vector2Int>> islands = GetFloatingIslands();
            if (islands.Count == 0) break;

            yield return StartCoroutine(DropFloatingIslands(islands));

            yield return new WaitForSeconds(0.3f);

            int destroyed = ClearLines();
            if (destroyed > 0 && !skipDestroyedNotification && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyed);

            if (destroyed == 0) break;
        }

        isProcessingIslands = false;
        SpawnPiece();
    }

    // ==================================================
    // 行シフト処理
    // ==================================================
    void ApplyGravity()
    {
        for (int repeat = 0; repeat < hight; repeat++)
        {
            for (int y = 0; y < hight - 1; y++)
            {
                bool isEmpty = true;
                for (int x = 0; x < wide; x++)
                    if (field[y, x] != BlockType.Empty) { isEmpty = false; break; }

                if (!isEmpty) continue;

                for (int yy = y; yy < hight - 1; yy++)
                    for (int x = 0; x < wide; x++)
                        field[yy, x] = field[yy + 1, x];

                for (int x = 0; x < wide; x++)
                    field[hight - 1, x] = BlockType.Empty;
            }
        }
    }

    // ==================================================
    // EKeyBomb処理
    // ==================================================
    int ExplodeBombs()
    {
        int totalDestroyed = 0;
        bool changed = true;

        while (changed)
        {
            changed = false;
            for (int y = 0; y < hight; y++)
            {
                for (int x = 0; x < wide; x++)
                {
                    if (field[y, x] != BlockType.EKeyBomb) continue;

                    field[y, x] = BlockType.Empty;
                    totalDestroyed++;

                    int xStart = Mathf.Max(0, x - 2);
                    int xEnd = Mathf.Min(wide - 1, x + 2);
                    int yStart = Mathf.Max(0, y - 2);
                    int yEnd = Mathf.Min(hight - 1, y + 2);

                    for (int yy = yStart; yy <= yEnd; yy++)
                        for (int xx = xStart; xx <= xEnd; xx++)
                            totalDestroyed += DestroyCell(xx, yy);

                    changed = true;
                }
            }
        }
        return totalDestroyed;
    }

    // ==================================================
    // 1マス評価して消去
    // ==================================================
    int DestroyCell(int x, int y)
    {
        if (field[y, x] == BlockType.Empty) return 0;

        if (field[y, x] == BlockType.CrossBomb)
        {
            field[y, x] = BlockType.Empty;
            int count = 1;
            for (int yy = 0; yy < hight; yy++)
                count += DestroyCell(x, yy);
            for (int xx = 0; xx < wide; xx++)
                count += DestroyCell(xx, y);
            return count;
        }
        else
        {
            field[y, x] = BlockType.Empty;
            return 1;
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
        for (int i = 0; i < currentShape.Length; i++)
        {
            Vector2Int p = currentPos + currentShape[i];
            if (p.y < 0 || p.y >= hight) continue;

            BlockType blockType;
            if (currentType == (int)BlockType.EKeyBomb)
                blockType = BlockType.EKeyBomb;
            else if (crossBombBlockIndices.Contains(i))
                blockType = BlockType.CrossBomb;
            else
                blockType = (BlockType)(currentType + 1);

            field[p.y, p.x] = blockType;
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

            List<Vector2Int> crossBombs = new List<Vector2Int>();
            for (int x = 0; x < wide; x++)
                if (field[y, x] == BlockType.CrossBomb) crossBombs.Add(new Vector2Int(x, y));

            for (int x = 0; x < wide; x++)
                if (field[y, x] != BlockType.Empty) { field[y, x] = BlockType.Empty; totalDestroyed++; }

            for (int yy = y; yy < hight - 1; yy++)
                for (int x = 0; x < wide; x++)
                    field[yy, x] = field[yy + 1, x];

            for (int x = 0; x < wide; x++)
                field[hight - 1, x] = BlockType.Empty;

            foreach (var bomb in crossBombs)
            {
                for (int yy = 0; yy < hight; yy++)
                    totalDestroyed += DestroyCell(bomb.x, yy);
                for (int xx = 0; xx < wide; xx++)
                    totalDestroyed += DestroyCell(xx, bomb.y);
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
                    if (field[y, x] == BlockType.CrossBomb)
                        gridRenderers[y, x].material.color = Color.black;
                    else if (field[y, x] == BlockType.EKeyBomb)
                        gridRenderers[y, x].material.color = Color.red;
                    else if (field[y, x] == BlockType.Piece9)
                        gridRenderers[y, x].material.color = Color.white;
                    else
                        gridRenderers[y, x].material.color = pieceColors[(int)field[y, x] - 1];
                }
            }
        }

        // 落下中ピース描画
        for (int i = 0; i < currentShape.Length; i++)
        {
            Vector2Int p = currentPos + currentShape[i];
            if (p.y >= 0 && p.y < hight)
            {
                gridObjects[p.y, p.x].SetActive(true);

                Color blockColor;
                if (crossBombBlockIndices.Contains(i))
                    blockColor = Color.black;
                else if (currentType == (int)BlockType.EKeyBomb)
                    blockColor = Color.red;
                else if (currentType == (int)BlockType.Piece9 - 1)
                    blockColor = Color.white;
                else
                    blockColor = pieceColors[currentType];

                gridRenderers[p.y, p.x].material.color = blockColor;
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

    // ==================================================
    // 浮き島検出
    // Flood Fillでy=0（床）に繋がっていないブロック群を浮き島と判定
    // ==================================================
    List<List<Vector2Int>> GetFloatingIslands()
    {
        bool[,] grounded = new bool[hight, wide];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int x = 0; x < wide; x++)
        {
            if (field[0, x] != BlockType.Empty)
            {
                grounded[0, x] = true;
                queue.Enqueue(new Vector2Int(x, 0));
            }
        }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            foreach (var d in dirs)
            {
                Vector2Int next = cell + d;
                if (next.x < 0 || next.x >= wide || next.y < 0 || next.y >= hight) continue;
                if (grounded[next.y, next.x]) continue;
                if (field[next.y, next.x] == BlockType.Empty) continue;

                grounded[next.y, next.x] = true;
                queue.Enqueue(next);
            }
        }

        bool[,] visited = new bool[hight, wide];
        List<List<Vector2Int>> islands = new List<List<Vector2Int>>();

        for (int y = 1; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                if (field[y, x] == BlockType.Empty) continue;
                if (grounded[y, x]) continue;
                if (visited[y, x]) continue;

                List<Vector2Int> island = new List<Vector2Int>();
                Queue<Vector2Int> islandQueue = new Queue<Vector2Int>();
                islandQueue.Enqueue(new Vector2Int(x, y));
                visited[y, x] = true;

                while (islandQueue.Count > 0)
                {
                    Vector2Int cell = islandQueue.Dequeue();
                    island.Add(cell);

                    foreach (var d in dirs)
                    {
                        Vector2Int next = cell + d;
                        if (next.x < 0 || next.x >= wide || next.y < 0 || next.y >= hight) continue;
                        if (visited[next.y, next.x]) continue;
                        if (field[next.y, next.x] == BlockType.Empty) continue;
                        if (grounded[next.y, next.x]) continue;

                        visited[next.y, next.x] = true;
                        islandQueue.Enqueue(next);
                    }
                }

                islands.Add(island);
            }
        }

        return islands;
    }

    // ==================================================
    // 浮き島落下アニメーション
    //
    // x列ごとにy昇順（下から）で着地座標を確定し、
    // 1秒かけてコマ送りで落下させる
    // ==================================================
    IEnumerator DropFloatingIslands(List<List<Vector2Int>> islands)
    {
        if (islands.Count == 0) yield break;

        // 全浮き島ブロックをSetにまとめる
        var islandSet = new HashSet<Vector2Int>();
        foreach (var island in islands)
            foreach (var b in island)
                islandSet.Add(b);

        // x列ごとにy昇順（下から上）で着地座標を確定
        // 下のブロックが着地した座標を次のブロックの障害物として扱う
        var finalPositions = new Dictionary<Vector2Int, Vector2Int>();

        for (int x = 0; x < wide; x++)
        {
            // この列の浮き島ブロックをy昇順で収集
            var colYs = new List<int>();
            foreach (var b in islandSet)
                if (b.x == x) colYs.Add(b.y);
            if (colYs.Count == 0) continue;
            colYs.Sort(); // y昇順（下から）

            // 最下段ブロックの直下にある障害物を探す（浮き島以外）
            int lowestY = colYs[0];
            int obstacleY = -1;
            for (int yy = lowestY - 1; yy >= 0; yy--)
            {
                if (field[yy, x] == BlockType.Empty) continue;
                if (islandSet.Contains(new Vector2Int(x, yy))) continue;
                obstacleY = yy;
                break;
            }

            // 下から順に着地座標を確定
            int nextLandY = obstacleY + 1;
            for (int i = 0; i < colYs.Count; i++)
            {
                finalPositions[new Vector2Int(x, colYs[i])] = new Vector2Int(x, nextLandY);
                nextLandY++;
            }
        }

        // 最大落下距離
        int maxDrop = 0;
        foreach (var kv in finalPositions)
        {
            int dist = kv.Key.y - kv.Value.y;
            if (dist > maxDrop) maxDrop = dist;
        }

        if (maxDrop == 0) yield break;

        float interval = 1.0f / maxDrop;

        for (int step = 0; step < maxDrop; step++)
        {
            // y昇順（下のブロックから）処理して上書きを防ぐ
            var allBlocks = new List<Vector2Int>(finalPositions.Keys);
            allBlocks.Sort((a, b) => a.y.CompareTo(b.y));

            foreach (var pos in allBlocks)
            {
                Vector2Int final = finalPositions[pos];
                int remaining = pos.y - final.y;
                if (remaining <= 0) continue;

                field[pos.y - 1, pos.x] = field[pos.y, pos.x];
                field[pos.y, pos.x] = BlockType.Empty;

                Vector2Int newPos = new Vector2Int(pos.x, pos.y - 1);
                finalPositions[newPos] = final;
                finalPositions.Remove(pos);

                // islandリストも更新
                foreach (var island in islands)
                {
                    int idx = island.IndexOf(pos);
                    if (idx >= 0) { island[idx] = newPos; break; }
                }
            }

            yield return new WaitForSeconds(interval);
        }
    }
}
