using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // さっき作った青いアイコンのEnemyを入れる枠
    public float spawnInterval = 3.0f; // 3秒おき
    float timer = 0;

    void Update()
    {
        timer += Time.deltaTime; // 時間をカウント
        if (timer >= spawnInterval)
        {
            Spawn();
            timer = 0;
        }
    }

    void Spawn()
    {
        // 床(30x30)の範囲内にランダムに座標を決める
        float x = Random.Range(-14f, 14f);
        float z = Random.Range(-14f, 14f);
        Vector3 pos = new Vector3(x, 0.5f, z);

        // 敵を召喚！
        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}