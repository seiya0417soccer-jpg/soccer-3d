using System.Collections.Generic;
using UnityEngine;

public class GumiData
{
    public Dictionary<int, Vector2Int[]> pieceData;
    public int PieceCount => pieceData.Count;

    public GumiData()
    {
#if true
        InitPieces();
#else
        InitDebugPieces();
#endif
    }

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
        pieceData[9] = new[] { new Vector2Int(0, 0) };                      // Piece9（白・1マス）
        pieceData[11] = new[] { new Vector2Int(0, 0) };                      // EKeyBomb（1マス）
    }

    void InitDebugPieces()
    {
        pieceData = new Dictionary<int, Vector2Int[]>();
        pieceData[0] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[1] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[2] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[3] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[4] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[5] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[6] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[7] = new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        pieceData[8] = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
    }

    public Color[] pieceColors =
    {
        Color.cyan, Color.blue, Color.green, Color.red, Color.yellow,
        Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
        Color.black, Color.black
    };
}