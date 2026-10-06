using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemySpawner.cs
/// 敵をフィールドに生成・管理するクラス
/// 
/// - EnemySettingSOからパラメーターを取得する（プランナーが調整可能）
/// - 敵が倒されたらOnEnemyDefeated()で通知を受けてスポーン
/// - Updateで毎フレームチェックする方式をやめてイベント駆動に変更
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // 敵のプレハブ
    [SerializeField] private GameObject _enemyPrefab;

    // プランナーが調整できるパラメーターをSOで管理
    [SerializeField] private EnemySettingSO _enemySettingSO;

    // 生存中の敵リスト
    private List<GameObject> _enemies = new List<GameObject>();

    // ==================================================
    // Start: 初回スポーン
    // ==================================================
    void Start()
    {
        for (int i = 0; i < _enemySettingSO.MaxEnemies; i++)
            Spawn();
    }

    // ==================================================
    // OnEnemyDefeated: 敵が倒された時に呼ぶ
    // YushaBrainから倒した敵を渡してもらい、リストから除去して新しい敵をスポーン
    // ==================================================
    public void OnEnemyDefeated(GameObject defeatedEnemy)
    {
        _enemies.Remove(defeatedEnemy);

        if (_enemies.Count < _enemySettingSO.MaxEnemies)
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

        float x = Random.Range(-_enemySettingSO.SpawnRange, _enemySettingSO.SpawnRange);
        float z = Random.Range(-_enemySettingSO.SpawnRange, _enemySettingSO.SpawnRange);
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

        for (int i = 0; i < _enemySettingSO.MaxEnemies; i++)
            Spawn();
    }
}