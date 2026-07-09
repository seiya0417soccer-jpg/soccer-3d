using UnityEngine;

/// <summary>
/// EnemySettingSO.cs
/// 敵のスポーン設定をScriptableObjectで管理する
/// 
/// - プランナーがコードを触らずにパラメーターを調整できる
/// - EnemySpawnerから参照して使う
/// </summary>
[CreateAssetMenu(fileName = "EnemySettingSO", menuName = "Settings/EnemySettingSO")]
public class EnemySettingSO : ScriptableObject
{
    [SerializeField] private int _maxEnemies = 3;      // 同時に存在する敵の最大数
    [SerializeField] private float _spawnRange = 14f;  // スポーン範囲

    public int MaxEnemies => _maxEnemies;
    public float SpawnRange => _spawnRange;
}