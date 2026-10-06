using UnityEngine;

/// <summary>
/// PuzzleRenderer.cs
/// パズルフィールドの描画専用クラス
/// 
/// - DropPuzzleBattleからDraw()の責務を分離
/// - フィールドの状態（BlockType配列）を受け取って描画するだけ
/// - ゲームロジックは一切持たない（単一責任原則）
/// </summary>
public class PuzzleRenderer
{
    // グリッドオブジェクト・Rendererの参照（DropPuzzleBattleから渡される）
    private readonly GameObject[,] _gridObjects;
    private readonly Renderer[,] _gridRenderers;
    private readonly Color[] _pieceColors;

    // ==================================================
    // コンストラクタ：描画に必要な参照を受け取る
    // ==================================================
    public PuzzleRenderer(GameObject[,] gridObjects, Renderer[,] gridRenderers, Color[] pieceColors)
    {
        _gridObjects = gridObjects;
        _gridRenderers = gridRenderers;
        _pieceColors = pieceColors;
    }

    // ==================================================
    // フィールド全体を描画する
    // ==================================================
    public void DrawField(DropPuzzleBattle.BlockType[,] field, int hight, int wide)
    {
        for (int y = 0; y < hight; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                if (field[y, x] == DropPuzzleBattle.BlockType.Empty)
                {
                    _gridObjects[y, x].SetActive(false); // 空マスは非表示
                }
                else
                {
                    _gridObjects[y, x].SetActive(true);
                    _gridRenderers[y, x].material.color = GetBlockColor(field[y, x]);
                }
            }
        }
    }

    // ==================================================
    // 落下中ピースを描画する（フィールドに固定される前のプレビュー）
    // ==================================================
    public void DrawCurrentPiece(
        Vector2Int currentPos,
        Vector2Int[] currentShape,
        int currentType,
        System.Collections.Generic.HashSet<int> crossBombBlockIndices,
        int hight)
    {
        for (int i = 0; i < currentShape.Length; i++)
        {
            Vector2Int p = currentPos + currentShape[i];
            if (p.y >= 0 && p.y < hight)
            {
                _gridObjects[p.y, p.x].SetActive(true);

                Color blockColor;
                if (crossBombBlockIndices.Contains(i))
                    blockColor = Color.black;              // CrossBombマーク付きは黒
                else if (currentType == (int)DropPuzzleBattle.BlockType.EKeyBomb)
                    blockColor = Color.red;                // EKeyBombは赤
                else if (currentType == (int)DropPuzzleBattle.BlockType.Piece9 - 1)
                    blockColor = Color.white;              // Piece9は白
                else
                    blockColor = _pieceColors[currentType]; // 通常はpiece色

                _gridRenderers[p.y, p.x].material.color = blockColor;
            }
        }
    }

    // ==================================================
    // ブロック種別から色を取得する
    // ==================================================
    private Color GetBlockColor(DropPuzzleBattle.BlockType blockType)
    {
        if (blockType == DropPuzzleBattle.BlockType.CrossBomb)
            return Color.black;
        if (blockType == DropPuzzleBattle.BlockType.EKeyBomb)
            return Color.red;
        if (blockType == DropPuzzleBattle.BlockType.Piece9)
            return Color.white;

        return _pieceColors[(int)blockType - 1];
    }
}