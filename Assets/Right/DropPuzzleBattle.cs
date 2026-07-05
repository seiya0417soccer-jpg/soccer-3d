using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DropPuzzleBattle.cs
/// テトリス型パズルバトルのメインロジック
/// 
/// 主な機能：
/// - BlockType enumでブロック種別を管理
/// - ピースの落下・移動・回転・固定
/// - ライン消去（CrossBomb起爆→消去→シフトの順で処理）
/// - EKeyBomb爆発（5×5範囲・連鎖対応）
/// - 浮き島検出（FloodFill）→落下アニメーション→連鎖消去
/// - ゲームオーバー検出→GameFlowManagerへ通知
/// </summary>
public class DropPuzzleBattle : MonoBehaviour
{
    // ==================================================
    // BlockType enum: フィールド上のブロック種別
    // Empty=空マス、Piece1〜9=通常ブロック
    // CrossBomb=十字爆弾（黒・1マス）、EKeyBomb=Eキー爆弾（赤・5×5範囲爆発）
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

    // グリッド表示用Prefab（typo修正：GridPrefub → _gridPrefab）
    [SerializeField] private GameObject _gridPrefab;

    // フィールドサイズ定義（ScriptableObject）
    [SerializeField] private PuzzleFieldSO puzzleFieldSO;

    int hight => puzzleFieldSO.Hight; // フィールドの高さ
    int wide => puzzleFieldSO.Wide;   // フィールドの幅

    // ピース形状・色データ
    private GumiData gumiData = new();

    // フィールド状態保持
    private BlockType[,] field;         // 各マスのブロック種別
    private GameObject[,] gridObjects;  // 各マスの表示オブジェクト
    private Renderer[,] gridRenderers;  // 各マスのRenderer（色変更用）

    // ピース形状データ（GumiDataから取得）
    private Dictionary<int, Vector2Int[]> pieceData => gumiData.pieceData;

    // ピース固定時に外部へ通知するイベント
    public event System.Action OnPieceFixed;

    // 落下中ピースの状態
    private Vector2Int[] currentShape; // 現在のピース形状（相対座標配列）
    private Vector2Int currentPos;     // 現在のピース基準座標
    private int currentType;           // 現在のピース種別（0〜11）

    // 落下タイマー
    private float fallTimer;           // 落下タイマー（経過時間）
    private float fallInterval = 1f;   // 通常落下間隔（秒）

    // 描画オフセット（テトリス画面をX=100付近に配置）
    private float offsetX = 100f;
    private float offsetY = 0f;

    // その他制御フラグ
    private bool skipDestroyedNotification = false; // ブロック消去通知をスキップするフラグ
    private int forcedNextPieceType = -1;           // 次のピース種別を強制指定（-1=通常）

    // CrossBombになるブロックインデックスの確率
    [SerializeField, Range(0f, 1f)] private float crossBombChance = 0.1f;

    // ピースの色配列（GumiDataから取得）
    private Color[] pieceColors => gumiData.pieceColors;

    // DropLogicExtensionへの参照（EKeyBomb発動などの拡張ロジック）
    private DropLogicExtension cachedLogic;

    // ==================================================
    // Start: 初期化
    // フィールド・グリッドオブジェクト生成、壁生成、最初のピース生成
    // ==================================================
    void Start()
    {
        // フィールド配列を初期化
        field = new BlockType[hight, wide];
        gridObjects = new GameObject[hight, wide];
        gridRenderers = new Renderer[hight, wide];

        // 各マスにグリッドオブジェクトを生成して配置
        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                GameObject obj = Instantiate(_gridPrefab);
                obj.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
                obj.SetActive(false); // 初期は非表示
                gridObjects[y, x] = obj;
                gridRenderers[y, x] = obj.GetComponent<Renderer>();
            }
        }

        // DropLogicExtensionをキャッシュ（毎フレーム検索しないため）
        cachedLogic = FindObjectOfType<DropLogicExtension>();

        CreateWall(); // フィールド外周の壁を生成
        SpawnPiece(); // 最初のピースを生成
    }

    // ==================================================
    // 壁生成
    // フィールドの外周（上下左右）にグレーのブロックを配置
    // ==================================================
    void CreateWall()
    {
        // 左右の壁
        for (int y = -1; y <= hight; y++)
        {
            CreateWallBlock(-1, y);
            CreateWallBlock(wide, y);
        }
        // 上下の壁
        for (int x = -1; x <= wide; x++)
        {
            CreateWallBlock(x, -1);
            CreateWallBlock(x, hight);
        }
    }

    // 壁ブロックを1マス生成してグレーに着色
    void CreateWallBlock(int x, int y)
    {
        GameObject wall = Instantiate(_gridPrefab);
        wall.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
        wall.GetComponent<Renderer>().material.color = Color.gray;
    }

    // ==================================================
    // Update: 毎フレーム処理
    // 落下タイマー更新・キー入力受付・描画更新
    // ==================================================
    void Update()
    {
        fallTimer += Time.deltaTime;

        // Sキー押しっぱなしで高速落下
        float speed = Input.GetKey(KeyCode.S) ? 0.05f : fallInterval;

        // 落下タイマーが閾値を超えたら1マス落下
        if (fallTimer >= speed)
        {
            fallTimer = 0;
            Move(Vector2Int.down);
        }

        // A/Dで左右移動、Wで回転
        if (Input.GetKeyDown(KeyCode.A)) Move(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) Move(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) Rotate();

        Draw(); // フィールドと落下中ピースを描画
    }

    // CrossBombになるブロックのインデックスセット
    private HashSet<int> crossBombBlockIndices = new HashSet<int>();

    // ==================================================
    // ピース生成
    // ランダム（またはExtensionから指定）でピースを生成して上部に配置
    // 生成位置が埋まっていたらゲームオーバー
    // ==================================================
    void SpawnPiece()
    {
        crossBombBlockIndices.Clear();

        // 強制指定されたピース種別があれば優先
        if (forcedNextPieceType != -1)
        {
            currentType = forcedNextPieceType;
            forcedNextPieceType = -1;
        }
        else
        {
            // ランダムでピース種別を決定（Extensionがあればそちらで上書き可）
            int defaultType = Random.Range(0, 9);
            currentType = cachedLogic != null
                ? cachedLogic.GetNextPieceType(defaultType)
                : defaultType;
        }

        currentShape = pieceData[currentType];

        // EKeyBomb以外のピースはランダムにCrossBombマークを付与
        if (currentType != (int)BlockType.EKeyBomb)
        {
            for (int i = 0; i < currentShape.Length; i++)
                if (Random.value < crossBombChance)
                    crossBombBlockIndices.Add(i);
        }

        // ピースをフィールド上部中央に配置
        currentPos = new Vector2Int(wide / 2, hight - 2);

        // 配置位置が有効でなければゲームオーバー
        if (!IsValidPosition(currentPos, currentShape))
        {
            Debug.Log("Game Over");
            enabled = false; // このスクリプトのUpdateを止める
            GameFlowManager.Instance?.OnGameOver();
        }
    }

    // 浮き島処理中フラグ（処理中はキー入力と落下を止める）
    private bool isProcessingIslands = false;

    // ==================================================
    // 移動
    // dir方向に移動を試みる
    // 下方向で移動不可なら固定→消去→浮き島処理→次のピース生成
    // ==================================================
    void Move(Vector2Int dir)
    {
        if (isProcessingIslands) return; // 浮き島処理中は入力を無視
        if (Time.timeScale == 0f) return; // 一時停止中は入力を無視

        Vector2Int newPos = currentPos + dir;

        if (IsValidPosition(newPos, currentShape))
        {
            // 移動可能なら座標を更新
            currentPos = newPos;
        }
        else if (dir == Vector2Int.down)
        {
            // 下方向に移動不可→ピースを固定して処理開始
            FixPiece();

            // ライン消去とEKeyBomb爆発を実行
            int destroyedBlocks = ClearLines();
            int bombDestroyed = ExplodeBombs();
            bool eKeyHit = bombDestroyed > 0;
            destroyedBlocks += bombDestroyed;

            // EKeyBombが爆発した場合はデバフを適用
            if (eKeyHit && BattleMainManager.Instance != null)
                BattleMainManager.Instance.ApplyEKeyDebuff(5f);

            cachedLogic?.OnEKeyBombFinished();

            // 消去ブロック数をBattleMainManagerに通知（バフ計算用）
            if (!skipDestroyedNotification && destroyedBlocks > 0 && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);

            // 浮き島処理→連鎖消去→次のピース生成のコルーチン開始
            StartCoroutine(ProcessIslandsThenSpawn());
        }
    }

    // ==================================================
    // 浮き島処理 → ライン消去連鎖 → SpawnPiece
    // 浮き島がなくなるまたは消去が発生しなくなるまでループ
    // ==================================================
    IEnumerator ProcessIslandsThenSpawn()
    {
        isProcessingIslands = true;

        while (true)
        {
            // 浮き島を検出
            List<List<Vector2Int>> islands = GetFloatingIslands();
            if (islands.Count == 0) break; // 浮き島がなければ終了

            // 浮き島を落下アニメーションで着地させる（1秒かけてコマ送り）
            yield return StartCoroutine(DropFloatingIslands(islands));

            // 着地後に少し待機（視認性のため）
            yield return new WaitForSeconds(0.3f);

            // 着地後にラインが揃っていれば消去（連鎖）
            int destroyed = ClearLines();
            if (destroyed > 0 && !skipDestroyedNotification && BattleMainManager.Instance != null)
                BattleMainManager.Instance.OnBlocksDestroyed(destroyed);

            if (destroyed == 0) break; // 消去がなければ連鎖終了
        }

        isProcessingIslands = false;
        SpawnPiece(); // 次のピースを生成
    }

    // ==================================================
    // 行シフト処理（ApplyGravity）
    // 空行を詰める処理（現在は浮き島落下で代替しているため未使用）
    // ==================================================
    void ApplyGravity()
    {
        for (int repeat = 0; repeat < hight; repeat++)
        {
            for (int y = 0; y < hight - 1; y++)
            {
                // y行が空かチェック
                bool isEmpty = true;
                for (int x = 0; x < wide; x++)
                    if (field[y, x] != BlockType.Empty) { isEmpty = false; break; }

                if (!isEmpty) continue;

                // y行が空なら上の行を全て1段下にシフト
                for (int yy = y; yy < hight - 1; yy++)
                    for (int x = 0; x < wide; x++)
                        field[yy, x] = field[yy + 1, x];

                // 最上行を空にする
                for (int x = 0; x < wide; x++)
                    field[hight - 1, x] = BlockType.Empty;
            }
        }
    }

    // ==================================================
    // EKeyBomb爆発処理
    // フィールド上のEKeyBombを検索し、見つかったら5×5範囲を爆発
    // 爆発で新たなEKeyBombが露出する場合は連鎖（whileループ）
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

                    // EKeyBomb自身を消去
                    field[y, x] = BlockType.Empty;
                    totalDestroyed++;

                    // 5×5範囲（上下左右2マス）を計算
                    int xStart = Mathf.Max(0, x - 2);
                    int xEnd = Mathf.Min(wide - 1, x + 2);
                    int yStart = Mathf.Max(0, y - 2);
                    int yEnd = Mathf.Min(hight - 1, y + 2);

                    // 範囲内の全マスを消去（CrossBombは連鎖起爆）
                    for (int yy = yStart; yy <= yEnd; yy++)
                        for (int xx = xStart; xx <= xEnd; xx++)
                            totalDestroyed += DestroyCell(xx, yy);

                    changed = true; // 変化があったので再スキャン
                }
            }
        }
        return totalDestroyed;
    }

    // ==================================================
    // 1マス消去処理
    // CrossBombの場合は十字方向に連鎖消去
    // 通常ブロックはそのまま消去して1を返す
    // ==================================================
    int DestroyCell(int x, int y)
    {
        if (field[y, x] == BlockType.Empty) return 0; // 空マスは何もしない

        if (field[y, x] == BlockType.CrossBomb)
        {
            // CrossBombは十字方向（同列・同行）を全て消去
            field[y, x] = BlockType.Empty;
            int count = 1;
            for (int yy = 0; yy < hight; yy++)
                count += DestroyCell(x, yy); // 同列を消去
            for (int xx = 0; xx < wide; xx++)
                count += DestroyCell(xx, y); // 同行を消去
            return count;
        }
        else
        {
            // 通常ブロックはそのまま消去
            field[y, x] = BlockType.Empty;
            return 1;
        }
    }

    // ==================================================
    // 回転
    // 現在のピースを90度回転させる（反時計回り）
    // 回転後の位置が有効でなければ回転しない
    // ==================================================
    void Rotate()
    {
        if (Time.timeScale == 0f) return; // 一時停止中は回転不可

        // 各ブロックを回転（x,y → -y,x）
        Vector2Int[] rotated = new Vector2Int[currentShape.Length];
        for (int i = 0; i < currentShape.Length; i++)
            rotated[i] = new Vector2Int(-currentShape[i].y, currentShape[i].x);

        // 回転後の位置が有効なら適用
        if (IsValidPosition(currentPos, rotated))
            currentShape = rotated;
    }

    // ==================================================
    // 有効位置判定
    // 指定座標と形状がフィールド内かつ他ブロックと重ならないか確認
    // ==================================================
    bool IsValidPosition(Vector2Int pos, Vector2Int[] shape)
    {
        foreach (var block in shape)
        {
            Vector2Int p = pos + block;
            if (p.x < 0 || p.x >= wide) return false;                            // 左右壁外
            if (p.y < 0) return false;                                            // 床より下
            if (p.y < hight && field[p.y, p.x] != BlockType.Empty) return false; // 他ブロックと重複
        }
        return true;
    }

    // ==================================================
    // ピース固定
    // 現在のピースをフィールドに書き込む
    // CrossBombマーク付きのブロックはCrossBombとして書き込む
    // ==================================================
    void FixPiece()
    {
        for (int i = 0; i < currentShape.Length; i++)
        {
            Vector2Int p = currentPos + currentShape[i];
            if (p.y < 0 || p.y >= hight) continue; // フィールド外は無視

            BlockType blockType;
            if (currentType == (int)BlockType.EKeyBomb)
                blockType = BlockType.EKeyBomb;      // EKeyBombはそのまま
            else if (crossBombBlockIndices.Contains(i))
                blockType = BlockType.CrossBomb;     // CrossBombマーク付きはCrossBomb
            else
                blockType = (BlockType)(currentType + 1); // 通常ブロック（+1でenum値に変換）

            field[p.y, p.x] = blockType;
        }
        OnPieceFixed?.Invoke(); // 固定完了を外部に通知
    }

    // ==================================================
    // ライン消去
    // 揃った行を検出し、CrossBomb起爆→消去→シフトの順で処理
    // 消去後に新たに揃う行が生まれる可能性があるのでwhileループで連鎖対応
    // ==================================================
    int ClearLines()
    {
        int totalDestroyed = 0;
        bool changed = true;

        while (changed)
        {
            changed = false;

            for (int y = 0; y < hight; y++)
            {
                // y行が全て埋まっているか確認
                bool full = true;
                for (int x = 0; x < wide; x++)
                    if (field[y, x] == BlockType.Empty) { full = false; break; }
                if (!full) continue;

                changed = true; // 揃った行があった

                // ① シフト前にCrossBombを起爆（シフト後だと座標がズレるため）
                for (int x = 0; x < wide; x++)
                {
                    if (field[y, x] != BlockType.CrossBomb) continue;
                    field[y, x] = BlockType.Empty;
                    totalDestroyed++;
                    for (int yy = 0; yy < hight; yy++)
                        totalDestroyed += DestroyCell(x, yy);
                    for (int xx = 0; xx < wide; xx++)
                        totalDestroyed += DestroyCell(xx, y);
                }

                // ② 揃った行の残りブロックを消去
                for (int x = 0; x < wide; x++)
                    if (field[y, x] != BlockType.Empty) { field[y, x] = BlockType.Empty; totalDestroyed++; }

                // ③ 上の行を1段下にシフト
                for (int yy = y; yy < hight - 1; yy++)
                    for (int x = 0; x < wide; x++)
                        field[yy, x] = field[yy + 1, x];

                // 最上行を空にする
                for (int x = 0; x < wide; x++)
                    field[hight - 1, x] = BlockType.Empty;

                y--; // シフトで行がずれるため再チェック
            }
        }

        return totalDestroyed;
    }

    // ==================================================
    // 描画
    // フィールドの全マスと落下中ピースをグリッドオブジェクトに反映
    // ==================================================
    void Draw()
    {
        // フィールドの全マスを描画
        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                if (field[y, x] == BlockType.Empty)
                {
                    gridObjects[y, x].SetActive(false); // 空マスは非表示
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

        // 落下中ピースを描画（フィールドに固定される前のプレビュー）
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
    // trueにするとOnBlocksDestroyedをBattleMainManagerに送らない
    // ==================================================
    public void SetSkipDestroyedNotification(bool value)
    {
        skipDestroyedNotification = value;
    }

    // ==================================================
    // 次ピース強制設定
    // 外部から次のピース種別を指定する（DropLogicExtensionなどから呼ぶ）
    // ==================================================
    public void ForceNextPieceType(int type)
    {
        forcedNextPieceType = type;
    }

    // ==================================================
    // ゲームリセット
    // もう一度プレイ時にGameFlowManagerから呼ぶ
    // フィールドをクリアして最初のピースから再開
    // ==================================================
    public void ResetGame()
    {
        // フィールドを全て空にする
        for (int y = 0; y < hight; y++)
            for (int x = 0; x < wide; x++)
                field[y, x] = BlockType.Empty;

        // グリッドオブジェクトを全て非表示にする（残像を消す）
        for (int y = 0; y < hight; y++)
            for (int x = 0; x < wide; x++)
                gridObjects[y, x].SetActive(false);

        isProcessingIslands = false; // 浮き島処理フラグをリセット
        fallTimer = 0f;              // 落下タイマーをリセット
        enabled = true;              // Updateを再開（ゲームオーバーで止めていた場合）
        SpawnPiece();                // 最初のピースを生成
    }

    // ==================================================
    // 浮き島検出
    // FloodFillでy=0（床）または床ブロックに繋がっていないブロック群を検出
    // 繋がっていないブロック群＝「浮き島」として返す
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
    // ① x列ごとにy昇順で各ブロックの着地座標を確定
    // ② 最大落下距離を計算して1/maxDrop秒間隔でコマ送り
    // ③ 各ステップでy昇順（下から）に1マスずつ移動
    // ==================================================
    IEnumerator DropFloatingIslands(List<List<Vector2Int>> islands)
    {
        if (islands.Count == 0) yield break;

        var islandSet = new HashSet<Vector2Int>();
        foreach (var island in islands)
            foreach (var b in island)
                islandSet.Add(b);

        var finalPositions = new Dictionary<Vector2Int, Vector2Int>();

        for (int x = 0; x < wide; x++)
        {
            var colYs = new List<int>();
            foreach (var b in islandSet)
                if (b.x == x) colYs.Add(b.y);
            if (colYs.Count == 0) continue;
            colYs.Sort();

            int lowestY = colYs[0];
            int obstacleY = -1;
            for (int yy = lowestY - 1; yy >= 0; yy--)
            {
                if (field[yy, x] == BlockType.Empty) continue;
                if (islandSet.Contains(new Vector2Int(x, yy))) continue;
                obstacleY = yy;
                break;
            }

            int nextLandY = obstacleY + 1;
            for (int i = 0; i < colYs.Count; i++)
            {
                finalPositions[new Vector2Int(x, colYs[i])] = new Vector2Int(x, nextLandY);
                nextLandY++;
            }
        }

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