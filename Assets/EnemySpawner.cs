using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    [SerializeField] private int maxEnemies = 3;    // 同時に存在する敵の最大数
    [SerializeField] private float spawnRange = 11f;  // スポーン範囲

    // 生存中の敵リスト
    private List<GameObject> enemies = new List<GameObject>();

    void Update()
    {
        // nullになった敵（破壊済み）をリストから除去
        enemies.RemoveAll(e => e == null);

        // 最大数に達するまでスポーン
        while (enemies.Count < maxEnemies)
            Spawn();
    }

    void Spawn()
    {
        float x = Random.Range(-spawnRange, spawnRange);
        float z = Random.Range(-spawnRange, spawnRange);
        Vector3 pos = new Vector3(x, 0.5f, z);

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        enemies.Add(enemy);
    }
}
