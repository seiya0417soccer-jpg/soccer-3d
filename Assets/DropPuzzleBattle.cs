using System.Collections.Generic;
using UnityEngine;

public class DropPuzzleBattle : MonoBehaviour
{
    public GameObject GridPrefub;

    private const int Width = 13;
    private const int Height = 22;

    private int[,] field = new int[Height, Width];
    private GameObject[,] gridObjects;

    private const int PieceCount = 9;
    private Dictionary<int, Vector2Int[]> pieceData;

    private Vector2Int[] currentShape;
    private Vector2Int currentPos;
    private int currentType;

    private float fallTimer;
    private float fallInterval = 1f;

    private float offsetX = 100f;
    private float offsetY = 0f;

    private Color[] pieceColors =
    {
        Color.cyan,
        Color.blue,
        Color.green,
        Color.red,
        Color.yellow,
        Color.magenta,
        new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 1f),
        Color.black
    };

    void Start()
    {
        gridObjects = new GameObject[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                GameObject obj = Instantiate(GridPrefub);
                obj.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
                obj.SetActive(false);
                gridObjects[y, x] = obj;
            }

        CreateWall();
        InitPieces();
        SpawnPiece();
    }

    void CreateWall()
    {
        for (int y = -1; y <= Height; y++)
        {
            CreateWallBlock(-1, y);
            CreateWallBlock(Width, y);
        }

        for (int x = -1; x <= Width; x++)
        {
            CreateWallBlock(x, -1);
            CreateWallBlock(x, Height);
        }
    }

    void CreateWallBlock(int x, int y)
    {
        GameObject wall = Instantiate(GridPrefub);
        wall.transform.position = new Vector3(x + offsetX, y + offsetY, 0);
        wall.GetComponent<Renderer>().material.color = Color.gray;
    }

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

    void InitPieces()
    {
        pieceData = new Dictionary<int, Vector2Int[]>();

        pieceData[0] = new Vector2Int[] { new(0, 0), new(1, 0), new(1, 1), new(2, 1), new(2, 2) };
        pieceData[1] = new Vector2Int[] { new(0, 0), new(1, 0), new(1, 1) };

        pieceData[2] = new Vector2Int[] { new(-1, 0), new(0, 0), new(1, 0), new(2, 0) };
        pieceData[3] = new Vector2Int[] { new(-1, 0), new(0, 0), new(1, 0), new(1, 1) };
        pieceData[4] = new Vector2Int[] { new(0, 0), new(1, 0), new(-1, 1), new(0, 1) };
        pieceData[5] = new Vector2Int[] { new(-1, 0), new(0, 0), new(1, 0), new(0, 1) };
        pieceData[6] = new Vector2Int[] { new(-1, 0), new(0, 0), new(1, 0), new(-1, 1) };
        pieceData[7] = new Vector2Int[] { new(-1, 0), new(0, 0), new(0, 1), new(1, 1) };

        // 爆弾（黒 O型）
        pieceData[8] = new Vector2Int[]
        {
            new(0,0),
            new(1,0),
            new(0,1),
            new(1,1)
        };
    }

    void SpawnPiece()
    {
        currentType = Random.Range(0, PieceCount);
        currentShape = pieceData[currentType];
        currentPos = new Vector2Int(Width / 2, Height - 2);

        if (!IsValidPosition(currentPos, currentShape))
        {
            Debug.Log("Game Over");
            enabled = false;
        }
    }

    void Move(Vector2Int dir)
    {
        Vector2Int newPos = currentPos + dir;

        if (IsValidPosition(newPos, currentShape))
        {
            currentPos = newPos;
        }
        else if (dir == Vector2Int.down)
        {
            FixPiece();

            // ★ 修正版：実際に消えたマス数を取得
            int destroyedBlocks = ClearLines();

            if (destroyedBlocks > 0)
            {
                BattleMainManager.Instance.OnBlocksDestroyed(destroyedBlocks);
            }

            SpawnPiece();
        }
    }

    void Rotate()
    {
        Vector2Int[] rotated = new Vector2Int[currentShape.Length];
        for (int i = 0; i < currentShape.Length; i++)
            rotated[i] = new Vector2Int(-currentShape[i].y, currentShape[i].x);

        if (IsValidPosition(currentPos, rotated))
            currentShape = rotated;
    }

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

    void FixPiece()
    {
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y < Height)
                field[p.y, p.x] = currentType + 1;
        }
    }

    // ★ 完全修正版：実際に消えたマス数を正確に返す
    int ClearLines()
    {
        int totalDestroyed = 0;

        for (int y = 0; y < Height; y++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (field[y, x] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                HashSet<int> bombColumns = new HashSet<int>();

                for (int x = 0; x < Width; x++)
                {
                    if (field[y, x] == 9)
                        bombColumns.Add(x);
                }

                // 横ライン削除（実在ブロックのみカウント）
                for (int x = 0; x < Width; x++)
                {
                    if (field[y, x] != 0)
                    {
                        field[y, x] = 0;
                        totalDestroyed++;
                    }
                }

                // 上を下にずらす
                for (int yy = y; yy < Height - 1; yy++)
                    for (int x = 0; x < Width; x++)
                        field[yy, x] = field[yy + 1, x];

                for (int x = 0; x < Width; x++)
                    field[Height - 1, x] = 0;

                // 爆弾縦爆発（穴あき対応）
                foreach (int col in bombColumns)
                {
                    for (int yy = 0; yy < Height; yy++)
                    {
                        if (field[yy, col] != 0)
                        {
                            field[yy, col] = 0;
                            totalDestroyed++;
                        }
                    }
                }

                y--;
            }
        }

        return totalDestroyed;
    }

    void Draw()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                gridObjects[y, x].SetActive(false);

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (field[y, x] != 0)
                {
                    gridObjects[y, x].SetActive(true);
                    gridObjects[y, x].GetComponent<Renderer>().material.color =
                        pieceColors[field[y, x] - 1];
                }

        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y < Height && p.y >= 0)
            {
                gridObjects[p.y, p.x].SetActive(true);
                gridObjects[p.y, p.x].GetComponent<Renderer>().material.color =
                    pieceColors[currentType];
            }
        }
    }
}