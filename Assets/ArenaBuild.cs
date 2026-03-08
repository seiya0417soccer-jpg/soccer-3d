using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ArenaBuilder.cs
/// 闘技場の外周壁をプロシージャルに生成する
/// 壁はスポーン範囲(±14f)の外側ぎりぎり(±15f)に配置
/// NavMeshObstacleをつけることで勇者・敵が壁の外に出ない
/// </summary>
public class ArenaBuilder : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField] private float arenaHalfSize = 15f;  // 壁の内側ハーフサイズ（スポーン範囲±14fより少し大きく）
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 2f;

    [Header("Materials")]
    [SerializeField] private Material wallMaterial;

    void Start()
    {
        BuildWalls();
    }

    void BuildWalls()
    {
        float half = arenaHalfSize;
        float wallY = wallHeight / 2f;
        float span = (half + wallThickness) * 2f; // 壁の長さ（角を塞ぐため少し長く）

        // 北壁（+Z）
        CreateWall(new Vector3(0, wallY, half + wallThickness / 2f), new Vector3(span, wallHeight, wallThickness));
        // 南壁（-Z）
        CreateWall(new Vector3(0, wallY, -half - wallThickness / 2f), new Vector3(span, wallHeight, wallThickness));
        // 東壁（+X）
        CreateWall(new Vector3(half + wallThickness / 2f, wallY, 0), new Vector3(wallThickness, wallHeight, span));
        // 西壁（-X）
        CreateWall(new Vector3(-half - wallThickness / 2f, wallY, 0), new Vector3(wallThickness, wallHeight, span));
    }

    void CreateWall(Vector3 position, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.transform.localScale = size;
        wall.name = "ArenaWall";

        // マテリアル適用
        if (wallMaterial != null)
            wall.GetComponent<Renderer>().material = wallMaterial;
        else
            wall.GetComponent<Renderer>().material.color = new Color(0.55f, 0.45f, 0.33f);

        // NavMeshObstacle: 勇者・敵がこの壁を越えられなくなる
        var obstacle = wall.AddComponent<NavMeshObstacle>();
        obstacle.carving = true; // NavMeshをリアルタイムに切り抜く
        obstacle.size = Vector3.one; // CubeのlocalScaleに合わせて自動計算される
    }
}

