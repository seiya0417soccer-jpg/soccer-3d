using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemySpawner.cs
/// 敵をフィールドに生成・管理するクラス
/// 
/// - 敵が倒されたらOnEnemyDefeated()で通知を受けてスポーン
/// - Updateで毎フレームチェックする方式をやめてイベント駆動に変更
/// - 最大数に達するまで初回スポーンはStart()で行う
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // 敵のプレハブ（SerializeField privateでカプセル化）
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _maxEnemies = 3;    // 同時に存在する敵の最大数
    [SerializeField] private float _spawnRange = 14f; // スポーン範囲

    // 生存中の敵リスト
    private List<GameObject> _enemies = new List<GameObject>();

    // ==================================================
    // Start: 初回スポーン
    // 最大数に達するまで敵を生成する
    // ==================================================
    void Start()
    {
        // 初回は最大数まで一気にスポーン
        for (int i = 0; i < _maxEnemies; i++)
            Spawn();
    }

    // ==================================================
    // OnEnemyDefeated: 敵が倒された時に呼ぶ
    // YushaBrainから倒した敵を渡してもらい、リストから除去して新しい敵をスポーン
    // ==================================================
    public void OnEnemyDefeated(GameObject defeatedEnemy)
    {
        // 倒された敵をリストから除去
        _enemies.Remove(defeatedEnemy);

        // 最大数に達していなければ新しい敵をスポーン
        if (_enemies.Count < _maxEnemies)
            Spawn();
    }

    // ==================================================
    // Spawn: 敵を1体生成してリストに追加
    // ==================================================
    void Spawn()
    {
        if (_enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefabがセットされていません！");
            return;
        }

        float x = Random.Range(-_spawnRange, _spawnRange);
        float z = Random.Range(-_spawnRange, _spawnRange);
        Vector3 pos = new Vector3(x, 0.5f, z);

        GameObject enemy = Instantiate(_enemyPrefab, pos, Quaternion.identity);
        _enemies.Add(enemy);
    }

    // ==================================================
    // ResetEnemies: 全敵を削除してリストをクリア
    // GameFlowManagerからリセット時に呼ぶ
    // ==================================================
    public void ResetEnemies()
    {
        foreach (var enemy in _enemies)
            if (enemy != null) Destroy(enemy);

        _enemies.Clear();

        // リセット後に最大数まで再スポーン
        for (int i = 0; i < _maxEnemies; i++)
            Spawn();
    }
}