using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlacementCandidateGenerator.cs
///
/// 盤面と現在のピース形状から、合法な配置候補を生成する。
///
/// 責務：
/// - ピースの回転候補を生成する
/// - 配置可能な列と着地点を計算する
/// - 配置後の盤面を簡易評価する
///
/// Jev APIやDropPuzzleBattleの具体型には依存しない。
/// 盤面情報はPuzzleAutomationSnapshotから取得する。
///
/// 注意：
/// - 爆弾の爆発・連鎖・浮き島処理はシミュレーションしない
/// - 評価値は基本的な配置判断を補助するための目安
/// </summary>
public sealed class PlacementCandidateGenerator
{
    // 盤面上の探索状態と、そこに到達するまでの移動手順
    private sealed class SearchNode
    {
        public Vector2Int Position;
        public int Rotation;
        public List<PuzzlePlacementAction> Actions;

        public SearchNode(
            Vector2Int position,
            int rotation,
            List<PuzzlePlacementAction> actions)
        {
            Position = position;
            Rotation = rotation;
            Actions = actions;
        }
    }

    // 各回転・基準X座標で採用する着地点
    private sealed class LandingInfo
    {
        public int Rotation;
        public int AnchorX;
        public int LandingY;
        public Vector2Int[] Shape;
        public PuzzlePlacementAction[] Actions;
    }

    /// <summary>
    /// 現在の盤面とピースから合法な配置候補を生成する。
    ///
    /// Rotationは現在のピース形状から反時計回りに
    /// 何回回転させるかを表す。
    /// </summary>
    public List<PuzzlePlacementCandidate> Generate(
    PuzzleAutomationSnapshot snapshot)
    {
        var candidates = new List<PuzzlePlacementCandidate>();

        if (snapshot == null ||
            snapshot.Width <= 0 ||
            snapshot.Height <= 0)
        {
            return candidates;
        }

        Vector2Int[] originalShape = snapshot.CopyCurrentShape();

        if (originalShape == null || originalShape.Length == 0)
            return candidates;

        Vector2Int startPosition = snapshot.CurrentPosition;
        int width = snapshot.Width;
        int height = snapshot.Height;

        if (!IsValidPosition(snapshot, startPosition, originalShape))
            return candidates;

        // 同じ位置・回転を重複して探索しない
        bool[,,] visited = new bool[height, width, 4];

        // 回転4方向 × 基準X座標ごとに、着地点を1つ保持する
        var bestLandings = new LandingInfo[4, width];

        var queue = new Queue<SearchNode>();

        var startNode = new SearchNode(
            startPosition,
            0,
            new List<PuzzlePlacementAction>());

        queue.Enqueue(startNode);
        visited[startPosition.y, startPosition.x, 0] = true;

        // 左右移動・下移動・回転を組み合わせて、到達可能な位置を探索する
        while (queue.Count > 0)
        {
            SearchNode node = queue.Dequeue();

            Vector2Int[] shape = GetRotatedShape(
                originalShape,
                node.Rotation);

            // 現在の位置で下へ移動できない場合、着地点として扱う
            bool canMoveDown = IsValidPosition(
                snapshot,
                node.Position + Vector2Int.down,
                shape);

            if (!canMoveDown &&
                node.Position.x >= 0 &&
                node.Position.x < width &&
                IsFullyInsideBoard(
                    width,
                    height,
                    node.Position,
                    shape))
            {
                int rotation = node.Rotation;
                int anchorX = node.Position.x;
                int landingY = node.Position.y;

                LandingInfo previous = bestLandings[rotation, anchorX];

                // 同じ回転・基準Xでは、最も低い着地点を採用する
                if (previous == null ||
                    landingY < previous.LandingY ||
                    (landingY == previous.LandingY &&
                     node.Actions.Count < previous.Actions.Length))
                {
                    bestLandings[rotation, anchorX] = new LandingInfo
                    {
                        Rotation = rotation,
                        AnchorX = anchorX,
                        LandingY = landingY,
                        Shape = shape,
                        Actions = node.Actions.ToArray()
                    };
                }
            }

            // 左右移動・下移動を探索する
            TryEnqueue(
                snapshot, node, node.Position + Vector2Int.left,
                node.Rotation, PuzzlePlacementAction.MoveLeft,
                originalShape, queue, visited);

            TryEnqueue(
                snapshot, node, node.Position + Vector2Int.right,
                node.Rotation, PuzzlePlacementAction.MoveRight,
                originalShape, queue, visited);

            TryEnqueue(
                snapshot, node, node.Position + Vector2Int.down,
                node.Rotation, PuzzlePlacementAction.MoveDown,
                originalShape, queue, visited);

            // 反時計回りに1回転する。回転後の位置が合法な場合だけ探索する
            int nextRotation = (node.Rotation + 1) % 4;

            TryEnqueue(
                snapshot, node, node.Position,
                nextRotation, PuzzlePlacementAction.RotateCounterClockwise,
                originalShape, queue, visited);
        }

        // 探索結果を配置候補に変換する
        for (int rotation = 0; rotation < 4; rotation++)
        {
            for (int anchorX = 0; anchorX < width; anchorX++)
            {
                LandingInfo landing = bestLandings[rotation, anchorX];

                if (landing == null)
                    continue;

                Vector2Int landingPosition = new Vector2Int(
                    landing.AnchorX,
                    landing.LandingY);

                EvaluatePlacement(
                    snapshot,
                    landingPosition,
                    landing.Shape,
                    out int directLinesCleared,
                    out int holesAfterClear,
                    out int aggregateHeightAfterClear,
                    out int maxHeightAfterClear);

                GetHorizontalBounds(
                    landing.Shape,
                    out int minX,
                    out int maxX);

                int leftmostColumn = landing.AnchorX + minX;

                // 回転・基準X・着地点を組み合わせて候補を識別する
                string candidateId =
                    $"R{rotation}_X{anchorX:D2}_Y{landing.LandingY:D2}";

                candidates.Add(
                    new PuzzlePlacementCandidate(
                        candidateId,
                        rotation,
                        leftmostColumn,
                        anchorX,
                        landing.LandingY,
                        directLinesCleared,
                        holesAfterClear,
                        aggregateHeightAfterClear,
                        maxHeightAfterClear,
                        landing.Actions));
            }
        }

        return candidates;
    }

    // 操作後の位置と回転が合法なら、次の探索対象として登録する
    private static void TryEnqueue(
        PuzzleAutomationSnapshot snapshot,
        SearchNode current,
        Vector2Int position,
        int rotation,
        PuzzlePlacementAction action,
        Vector2Int[] originalShape,
        Queue<SearchNode> queue,
        bool[,,] visited)
    {
        // 探索配列の範囲外にある基準座標は扱わない
        if (position.x < 0 || position.x >= snapshot.Width ||
            position.y < 0 || position.y >= snapshot.Height)
        {
            return;
        }

        Vector2Int[] shape = GetRotatedShape(originalShape, rotation);

        // 壁・床・固定ブロックに干渉する移動は候補にしない
        if (!IsValidPosition(snapshot, position, shape))
            return;

        // 同じ位置・回転は一度だけ探索する
        if (visited[position.y, position.x, rotation])
            return;

        visited[position.y, position.x, rotation] = true;

        var actions = new List<PuzzlePlacementAction>(current.Actions);
        actions.Add(action);

        queue.Enqueue(new SearchNode(position, rotation, actions));
    }

    // 開始時の形状を基準に、反時計回りの回転を必要な回数だけ適用する
    private static Vector2Int[] GetRotatedShape(
        Vector2Int[] originalShape,
        int rotation)
    {
        Vector2Int[] shape = (Vector2Int[])originalShape.Clone();

        for (int i = 0; i < rotation; i++)
        {
            shape = RotateShape(shape);
        }

        return shape;
    }

    /// <summary>
    /// 指定形状を90度反時計回りに回転する。
    /// </summary>
    private static Vector2Int[] RotateShape(Vector2Int[] shape)
    {
        var rotated = new Vector2Int[shape.Length];

        for (int i = 0; i < shape.Length; i++)
        {
            rotated[i] = new Vector2Int(
                -shape[i].y,
                shape[i].x);
        }

        return rotated;
    }

    /// <summary>
    /// 形状が占有するX座標の最小値・最大値を取得する。
    /// </summary>
    private static void GetHorizontalBounds(
        Vector2Int[] shape,
        out int minX,
        out int maxX)
    {
        minX = shape[0].x;
        maxX = shape[0].x;

        foreach (Vector2Int block in shape)
        {
            minX = Mathf.Min(minX, block.x);
            maxX = Mathf.Max(maxX, block.x);
        }
    }

    /// <summary>
    /// 指定位置に形状を置けるか判定する。
    /// 盤面上端より上のセルは、スポーン位置として許可する。
    /// </summary>
    private static bool IsValidPosition(
        PuzzleAutomationSnapshot snapshot,
        Vector2Int position,
        Vector2Int[] shape)
    {
        foreach (Vector2Int block in shape)
        {
            Vector2Int cell = position + block;

            // 左右の境界外や床より下には置けない。
            if (cell.x < 0 || cell.x >= snapshot.Width)
                return false;

            if (cell.y < 0)
                return false;

            // 盤面上端より上は空きとして扱う。
            if (cell.y >= snapshot.Height)
                continue;

            if (snapshot.IsOccupied(cell.x, cell.y))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 最終配置が盤面内に完全に収まるか確認する。
    /// </summary>
    private static bool IsFullyInsideBoard(
        int width,
        int height,
        Vector2Int position,
        Vector2Int[] shape)
    {
        foreach (Vector2Int block in shape)
        {
            Vector2Int cell = position + block;

            if (cell.x < 0 || cell.x >= width)
                return false;

            if (cell.y < 0 || cell.y >= height)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 候補を仮配置し、基本的な盤面評価値を計算する。
    ///
    /// 評価対象：
    /// - 直接完成する行数
    /// - 直接完成した行を除去した後の穴数
    /// - 列高さの合計
    /// - 最大列高さ
    ///
    /// 爆弾の爆発・連鎖・浮き島落下は計算しない。
    /// </summary>
    private static void EvaluatePlacement(
        PuzzleAutomationSnapshot snapshot,
        Vector2Int position,
        Vector2Int[] shape,
        out int directLinesCleared,
        out int holesAfterClear,
        out int aggregateHeightAfterClear,
        out int maxHeightAfterClear)
    {
        int width = snapshot.Width;
        int height = snapshot.Height;

        bool[,] simulated = snapshot.CopyOccupied();

        // 仮配置。呼び出し元のSnapshotは変更しない。
        foreach (Vector2Int block in shape)
        {
            Vector2Int cell = position + block;
            simulated[cell.y, cell.x] = true;
        }

        // この仮盤面で直接完成した行を数える。
        var fullRows = new bool[height];
        directLinesCleared = 0;

        for (int y = 0; y < height; y++)
        {
            bool isFull = true;

            for (int x = 0; x < width; x++)
            {
                if (simulated[y, x])
                    continue;

                isFull = false;
                break;
            }

            fullRows[y] = isFull;

            if (isFull)
                directLinesCleared++;
        }

        // 直接完成した行を除去し、残りの行を下から詰める。
        // ここでは連鎖消去は再計算しない。
        bool[,] compacted = new bool[height, width];
        int writeY = 0;

        for (int y = 0; y < height; y++)
        {
            if (fullRows[y])
                continue;

            for (int x = 0; x < width; x++)
            {
                compacted[writeY, x] = simulated[y, x];
            }

            writeY++;
        }

        holesAfterClear = 0;
        aggregateHeightAfterClear = 0;
        maxHeightAfterClear = 0;

        for (int x = 0; x < width; x++)
        {
            int columnHeight = 0;

            for (int y = height - 1; y >= 0; y--)
            {
                if (!compacted[y, x])
                    continue;

                columnHeight = y + 1;
                break;
            }

            aggregateHeightAfterClear += columnHeight;

            maxHeightAfterClear = Mathf.Max(
                maxHeightAfterClear,
                columnHeight);

            // 最上部のブロックより下にある空きマスを穴とする。
            for (int y = 0; y < columnHeight; y++)
            {
                if (!compacted[y, x])
                    holesAfterClear++;
            }
        }
    }
}