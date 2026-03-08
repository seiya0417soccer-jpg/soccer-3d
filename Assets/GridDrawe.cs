using UnityEngine;

/// <summary>
/// GridDrawer.cs
/// テトリスフィールドのマス目をLineRendererで描画する
/// Wide=13, Hight=22 に合わせた設定
/// </summary>
public class GridDrawer : MonoBehaviour
{
    [SerializeField] private PuzzleFieldSO puzzleFieldSO;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;       // 1マスのサイズ
    [SerializeField] private float offsetX = 100f;     // テトリスフィールドのXオフセット（DropPuzzleBattleに合わせる）
    [SerializeField] private float offsetY = 0f;       // テトリスフィールドのYオフセット
    [SerializeField] private float gridZ = 0f;       // Z座標

    [Header("Line Settings")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.2f); // 薄い白
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Material lineMaterial;      // UnlitマテリアルをInspectorからセット

    int wide => puzzleFieldSO.Wide;
    int hight => puzzleFieldSO.Hight;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        // 縦線（x方向）: wide+1本
        for (int x = 0; x <= wide; x++)
        {
            Vector3 start = new Vector3(x * cellSize + offsetX - cellSize / 2f, offsetY - cellSize / 2f, gridZ);
            Vector3 end = new Vector3(x * cellSize + offsetX - cellSize / 2f, offsetY + hight * cellSize - cellSize / 2f, gridZ);
            CreateLine(start, end);
        }

        // 横線（y方向）: hight+1本
        for (int y = 0; y <= hight; y++)
        {
            Vector3 start = new Vector3(offsetX - cellSize / 2f, y * cellSize + offsetY - cellSize / 2f, gridZ);
            Vector3 end = new Vector3(offsetX + wide * cellSize - cellSize / 2f, y * cellSize + offsetY - cellSize / 2f, gridZ);
            CreateLine(start, end);
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject obj = new GameObject("GridLine");
        obj.transform.parent = transform;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }
        else
        {
            // マテリアル未設定時はデフォルトのUnlitを使用
            lr.material = new Material(Shader.Find("Unlit/Color"));
        }

        lr.startColor = lineColor;
        lr.endColor = lineColor;
    }
}
