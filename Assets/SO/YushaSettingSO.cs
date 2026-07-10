using UnityEngine;

/// <summary>
/// YushaSettingSO.cs
/// 勇者のパラメーター設定をScriptableObjectで管理する
/// 
/// - プランナーがコードを触らずにパラメーターを調整できる
/// - YushaBrainから参照して使う
/// </summary>
[CreateAssetMenu(fileName = "YushaSettingSO", menuName = "Settings/YushaSettingSO")]
public class YushaSettingSO : ScriptableObject
{
    [SerializeField] private float _defaultSpeed = 2f;      // 勇者のデフォルト移動速度
    [SerializeField] private float _attackDistance = 2.5f;  // 攻撃範囲
    [SerializeField] private float _attackDelay = 0.3f;     // 攻撃モーション後にDestroyするまでの待機時間

    public float DefaultSpeed => _defaultSpeed;
    public float AttackDistance => _attackDistance;
    public float AttackDelay => _attackDelay;
}