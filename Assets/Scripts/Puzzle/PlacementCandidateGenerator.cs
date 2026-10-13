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

        if (snapshot == null)
            return candidates;

        int width = snapshot.Width;
        int height = snapshot.Height;

        if (width <= 0 || height <= 0)
            return candidates;

        Vector2Int[] originalShape = snapshot.CopyCurrentShape();

        if (originalShape == null || originalShape.Length == 0)
            return candidates;

        // Snapshotに保存された現在の形状を基準に回転する。
        Vector2Int[] rotatedShape =
            (Vector2Int[])originalShape.Clone();

        // 最大4方向。対称形状でも回転候補を維持する。
        // ブロックごとの特殊属性があるため、形状だけで重複除外しない。
        for (int rotation = 0; rotation < 4; rotation++)
        {
            GetHorizontalBounds(
                rotatedShape,
                out int minX,
                out int maxX);

            int pieceWidth = maxX - minX + 1;

            if (pieceWidth > 0 && pieceWidth <= width)
            {
                // 実際に占有する左端の列を基準に走査する。
                for (int leftmostColumn = 0;
                     leftmostColumn <= width - pieceWidth;
                     leftmostColumn++)
                {
                    // 形状の基準座標と占有範囲の左端を区別する。
                    int anchorX = leftmostColumn - minX;

                    // 現在のピースの高さから開始する。
                    // 横移動先で既存ブロックと重なる候補は除外する。
                    var spawnPosition = new Vector2Int(
                        anchorX,
                        snapshot.CurrentPosition.y);

                    if (!IsValidPosition(
                        snapshot,
                        spawnPosition,
                        rotatedShape))
                    {
                        continue;
                    }

                    // 真下に移動できなくなる位置まで落下させる。
                    int landingY = spawnPosition.y;

                    while (IsValidPosition(
                        snapshot,
                        new Vector2Int(anchorX, landingY - 1),
                        rotatedShape))
                    {
                        landingY--;
                    }

                    var landingPosition = new Vector2Int(
                        anchorX,
                        landingY);

                    // 最終配置が盤面内に完全に収まるか確認する。
                    if (!IsFullyInsideBoard(
                        width,
                        height,
                        landingPosition,
                        rotatedShape))
                    {
                        continue;
                    }

                    EvaluatePlacement(
                        snapshot,
                        landingPosition,
                        rotatedShape,
                        out int directLinesCleared,
                        out int holesAfterClear,
                        out int aggregateHeightAfterClear,
                        out int maxHeightAfterClear);

                    // 同じピース内で一意になる識別子。
                    string candidateId =
                        $"R{rotation}_L{leftmostColumn:D2}";

                    candidates.Add(
                        new PuzzlePlacementCandidate(
                            candidateId,
                            rotation,
                            leftmostColumn,
                            anchorX,
                            landingY,
                            directLinesCleared,
                            holesAfterClear,
                            aggregateHeightAfterClear,
                            maxHeightAfterClear));
                }
            }

            // DropPuzzleBattle.Rotate()と同じ回転規則。
            rotatedShape = RotateShape(rotatedShape);
        }

        return candidates;
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