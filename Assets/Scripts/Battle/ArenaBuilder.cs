using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ArenaBuilder.cs
/// 闘技場の外周壁をプロシージャルに生成する
/// 壁はスポーン範囲(±14f)の外側ぎりぎり(±15f)に配置
/// NavMeshObstacleをつけることで勇者・敵が壁の外に出ない
/// 壁生成と同時にPerlinノイズで石テクスチャを自動生成して適用
/// </summary>
public class ArenaBuilder : MonoBehaviour
{
    [Header("Arena Settings")]
    [SerializeField] private float arenaHalfSize = 15f; // 壁の内側ハーフサイズ（スポーン範囲±14fより少し大きく）
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 2f;

    [Header("Materials")]
    [SerializeField] private Material wallMaterial; // 設定されていればこちらを優先、なければ石テクスチャを自動生成

    [Header("Stone Texture Settings")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float noiseScale = 12f;
    [SerializeField] private Color colorLight = new Color(0.80f, 0.80f, 0.80f); // 明るいグレー
    [SerializeField] private Color colorMid = new Color(0.60f, 0.60f, 0.60f); // 中間グレー
    [SerializeField] private Color colorDark = new Color(0.40f, 0.40f, 0.40f); // 暗いグレー

    // 生成した石マテリアルを全壁で使い回す（毎回生成しない）
    private Material stoneMaterial;

    void Start()
    {
        // wallMaterialが未設定の場合はPerlinノイズで石テクスチャを生成
        if (wallMaterial == null)
        {
            stoneMaterial = new Material(Shader.Find("Standard"));
            stoneMaterial.mainTexture = GenerateStoneTexture();
        }

        BuildWalls();
    }

    // ==================================================
    // 外周4壁を生成
    // ==================================================
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

    // ==================================================
    // 壁を1枚生成してマテリアル・NavMeshObstacleを設定
    // ==================================================
    void CreateWall(Vector3 position, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.transform.localScale = size;
        wall.name = "ArenaWall";

        // マテリアル適用（Inspector設定 > 自動生成石テクスチャ の優先順）
        if (wallMaterial != null)
            wall.GetComponent<Renderer>().material = wallMaterial;
        else
            wall.GetComponent<Renderer>().material = stoneMaterial;

        // NavMeshObstacle: 勇者・敵がこの壁を越えられなくなる
        var obstacle = wall.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;      // NavMeshをリアルタイムに切り抜く
        obstacle.size = Vector3.one; // CubeのlocalScaleに合わせて自動計算される
    }

    // ==================================================
    // Perlinノイズで石テクスチャを生成
    // 2重ノイズで細かい石の模様を表現
    // ==================================================
    Texture2D GenerateStoneTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nx = (float)x / textureSize * noiseScale;
                float ny = (float)y / textureSize * noiseScale;
                float noise = Mathf.PerlinNoise(nx, ny);
                float noise2 = Mathf.PerlinNoise(nx * 2.5f, ny * 2.5f) * 0.3f;
                float combined = Mathf.Clamp01(noise + noise2 - 0.15f);

                Color color = Color.Lerp(colorDark, colorLight, combined);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}
