using System.Text;
using UnityEngine;

/// <summary>
/// PuzzleAutomationSnapshot.cs
///
/// 判断開始時点の盤面と現在のピースを保持する。
///
/// 配列は取得時にコピーする。
/// 判断中のゲーム状態が変化しても、取得済みの情報は維持される。
/// </summary>
public sealed class PuzzleAutomationSnapshot
{
    private readonly bool[,] _occupied;
    private readonly Vector2Int[] _currentShape;

    public int Width { get; }
    public int Height { get; }
    public int CurrentType { get; }
    public Vector2Int CurrentPosition { get; }

    public PuzzleAutomationSnapshot(
        int width,
        int height,
        int currentType,
        Vector2Int currentPosition,
        Vector2Int[] currentShape,
        bool[,] occupied)
    {
        Width = width;
        Height = height;
        CurrentType = currentType;
        CurrentPosition = currentPosition;

        _currentShape = (Vector2Int[])currentShape.Clone();
        _occupied = (bool[,])occupied.Clone();
    }

    /// <summary>
    /// 現在のピース形状をコピーして取得する。
    /// </summary>
    public Vector2Int[] CopyCurrentShape()
    {
        return (Vector2Int[])_currentShape.Clone();
    }

    /// <summary>
    /// 指定した座標が占有されているか確認する。
    /// </summary>
    public bool IsOccupied(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return false;
        }

        return _occupied[y, x];
    }

    /// <summary>
    /// 盤面をコピーして取得する。
    /// </summary>
    public bool[,] CopyOccupied()
    {
        return (bool[,])_occupied.Clone();
    }

    /// <summary>
    /// Jevに渡す盤面と現在のピースの説明を作成する。
    /// 特殊ブロックの効果は表現せず、占有状態のみを示す。
    /// </summary>
    public string ToStateDescription()
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine(
            $"盤面サイズ: 幅{Width}列、高さ{Height}行");

        builder.AppendLine(
            "盤面: # は占有マス、. は空きマス");

        for (int y = Height - 1; y >= 0; y--)
        {
            builder.Append($"Y{y:D2}|");

            for (int x = 0; x < Width; x++)
            {
                builder.Append(_occupied[y, x] ? '#' : '.');
            }

            builder.AppendLine();
        }

        builder.AppendLine($"現在のピース種別ID: {CurrentType}");
        builder.AppendLine("現在のピース形状（相対座標）:");

        foreach (Vector2Int block in _currentShape)
        {
            builder.Append($"({block.x},{block.y}) ");
        }

        return builder.ToString();
    }
}