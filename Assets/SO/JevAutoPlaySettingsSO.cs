using UnityEngine;

/// <summary>
/// JevAutoPlaySettingsSO.cs
///
/// Jev自動プレイ専用の設定。
/// 通常プレイの速度設定には影響しない。
/// </summary>
[CreateAssetMenu(
    fileName = "JevAutoPlaySettings",
    menuName = "Jev/Auto Play Settings")]
public sealed class JevAutoPlaySettingsSO : ScriptableObject
{
    [Header("落下設定")]

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("Jev自動プレイ時に1マス落下するまでの秒数。大きいほどゆっくり落下する。")]
    private float fallStepInterval = 1.0f;

    /// <summary>
    /// 1マス落下するまでの秒数。
    /// </summary>
    public float FallStepInterval => fallStepInterval;
}