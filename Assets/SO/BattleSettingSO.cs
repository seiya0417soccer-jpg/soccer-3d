using UnityEngine;

/// <summary>
/// BattleSettingSO.cs
/// バトルのバフ・デバフ設定をScriptableObjectで管理する
/// 
/// - プランナーがコードを触らずにパラメーターを調整できる
/// - BattleMainManagerから参照して使う
/// </summary>
[CreateAssetMenu(fileName = "BattleSettingSO", menuName = "Settings/BattleSettingSO")]
public class BattleSettingSO : ScriptableObject
{
    [SerializeField] private float _secondsPerBlock = 0.3f; // ブロック1個あたりのバフ持続時間
    [SerializeField] private float _speedPerBlock = 0.2f;   // ブロック1個あたりの速度バフ量
    [SerializeField] private float _eKeyDebuffDuration = 5f; // EKeyBombデバフの持続時間

    public float SecondsPerBlock => _secondsPerBlock;
    public float SpeedPerBlock => _speedPerBlock;
    public float EKeyDebuffDuration => _eKeyDebuffDuration;
}