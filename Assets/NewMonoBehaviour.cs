using System.Collections.Generic;
using UnityEngine;

public class NewTetris : MonoBehaviour
{
    public GameObject GridPrefub;

    private const int Width = 10;
    private const int Height = 20;

    private int[,] field = new int[Height, Width];
    private GameObject[,] gridObjects;

    private enum MinoType { I, J, L, T, O, S, Z }
    private Dictionary<MinoType, Vector2Int[]> minoData;

    private Vector2Int[] currentShape;
    private Vector2Int currentPos;
    private MinoType currentType;

    private float fallTimer;
    private float fallInterval = 1f;

    // 🔥 カメラが105にあるので合わせる
    private float offsetX = 100f;
    private float offsetY = 0f;

    private Color[] minoColors =
    {
        Color.cyan,
        Color.blue,
        new Color(1f,0.5f,0f),
        Color.magenta,
        Color.yellow,
        Color.green,
        Color.red
    };

    void Start()
    {
        gridObjects = new GameObject[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                GameObject obj = Instantiate(GridPrefub);
                obj.transform.position = new Vector3(
                    x + offsetX,
                    y + offsetY,
                    0
                );
                obj.SetActive(false);
                gridObjects[y, x] = obj;
            }
        }

        CreateWall();
        InitMino();
        SpawnMino();
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
        wall.transform.position = new Vector3(
            x + offsetX,
            y + offsetY,
            0
        );
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

        if (Input.GetKeyDown(KeyCode.A))
            Move(Vector2Int.left);

        if (Input.GetKeyDown(KeyCode.D))
            Move(Vector2Int.right);

        if (Input.GetKeyDown(KeyCode.W))
            Rotate();

        Draw();
    }

    void InitMino()
    {
        minoData = new Dictionary<MinoType, Vector2Int[]>();

        minoData[MinoType.I] = new Vector2Int[]
        {
            new Vector2Int(-1,0), new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(2,0)
        };

        minoData[MinoType.O] = new Vector2Int[]
        {
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        };

        minoData[MinoType.T] = new Vector2Int[]
        {
            new Vector2Int(-1,0), new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(0,1)
        };

        minoData[MinoType.J] = new Vector2Int[]
        {
            new Vector2Int(-1,0), new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(-1,1)
        };

        minoData[MinoType.L] = new Vector2Int[]
        {
            new Vector2Int(-1,0), new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(1,1)
        };

        minoData[MinoType.S] = new Vector2Int[]
        {
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(-1,1), new Vector2Int(0,1)
        };

        minoData[MinoType.Z] = new Vector2Int[]
        {
            new Vector2Int(-1,0), new Vector2Int(0,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        };
    }

    void SpawnMino()
    {
        currentType = (MinoType)Random.Range(0, 7);
        currentShape = minoData[currentType];
        currentPos = new Vector2Int(Width / 2, Height - 1);

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
            FixMino();
            ClearLines();
            SpawnMino();
        }
    }

    void Rotate()
    {
        if (currentType == MinoType.O) return;

        Vector2Int[] rotated = new Vector2Int[4];

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

    void FixMino()
    {
        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;
            if (p.y < Height)
                field[p.y, p.x] = (int)currentType + 1;
        }
    }

    void ClearLines()
    {
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
                for (int yy = y; yy < Height - 1; yy++)
                    for (int x = 0; x < Width; x++)
                        field[yy, x] = field[yy + 1, x];

                for (int x = 0; x < Width; x++)
                    field[Height - 1, x] = 0;

                y--;
            }
        }
    }

    void Draw()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                gridObjects[y, x].SetActive(false);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (field[y, x] != 0)
                {
                    gridObjects[y, x].SetActive(true);
                    gridObjects[y, x].GetComponent<Renderer>().material.color =
                        minoColors[field[y, x] - 1];
                }
            }
        }

        foreach (var block in currentShape)
        {
            Vector2Int p = currentPos + block;

            if (p.y < Height && p.y >= 0)
            {
                gridObjects[p.y, p.x].SetActive(true);
                gridObjects[p.y, p.x].GetComponent<Renderer>().material.color =
                    minoColors[(int)currentType];
            }
        }
    }
}