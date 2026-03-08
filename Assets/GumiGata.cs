using System.Collections.Generic;
using UnityEngine;

public class GumiData
{
    private Dictionary<int, Vector2Int[]> pieceData;       // ピース形状データ
    public GumiData()
    {
        InitPieces();
    }


    // ピース形状初期
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
        pieceData[9] = new[] { new Vector2Int(0, 0) };
    }

}