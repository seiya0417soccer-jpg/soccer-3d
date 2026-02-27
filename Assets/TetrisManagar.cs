using UnityEngine;

public class TetrisManager : MonoBehaviour
{
    public GameObject blockPrefab;   // 枠（壁）に使うCube
    public GameObject[] minoPrefabs; // 7種類のミノを入れる配列
    public static TetrisManager instance;
    public static Transform[,] grid = new Transform[12, 25];

    void Awake() { instance = this; }

    void Start()
    {
        // 1. 【復活】枠（壁）を作るロジック
        // 底を作る (X:0〜11)
        for (int x = 0; x <= 11; x++) CreateBlock(x, 0);
        // 左右の壁を作る (Y:1〜20)
        for (int y = 1; y <= 20; y++)
        {
            CreateBlock(0, y);  // 左壁
            CreateBlock(11, y); // 右壁
        }

        // 2. 最初のミノを召喚！
        SpawnMino();
    }

    // ミノをランダムに空から降らせる関数
    public void SpawnMino()
    {
        if (minoPrefabs.Length == 0) return; // 中身が空なら何もしない
        int index = Random.Range(0, minoPrefabs.Length);
        // 真ん中付近(105.5)の空(20)から召喚
        Instantiate(minoPrefabs[index], new Vector3(105.5f, 20f, 0f), Quaternion.identity);
    }

    void CreateBlock(int x, int y)
    {
        // X座標を100ずらして右画面専用エリアへ配置
        Vector3 pos = new Vector3(100 + x, y, 0);
        Instantiate(blockPrefab, pos, Quaternion.identity, transform);
    }
}