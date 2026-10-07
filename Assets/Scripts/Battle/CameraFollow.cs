using System.Collections;
using UnityEngine;

/// <summary>
/// CameraFollow.cs
/// 勇者を追いかけるカメラ制御
/// 
/// - 勇者の位置にオフセットを加えてカメラを追従させる
/// - ShakeCamera()で画面揺れを発生させる（敵を倒した時などに呼ぶ）
/// - StopShake()でシェイクを強制停止し、以降のシェイクも禁止する
///   （ゲームオーバー・フィニッシュ時に呼ぶ）
/// - EnableShake()で禁止を解除する（もう一度プレイ・タイトル復帰時に呼ぶ）
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // 追いかける対象（勇者）
    [SerializeField] private Transform _target;

    // 勇者からのオフセット距離
    [SerializeField] private Vector3 _offset = new Vector3(0, 15, -10);

    [Header("Camera Shake Settings")]
    [SerializeField] private float _shakeDuration = 0.2f;  // 揺れの時間
    [SerializeField] private float _shakeMagnitude = 0.3f; // 揺れの強さ

    // シェイク中フラグ
    private bool _isShaking = false;

    // シェイクを禁止するフラグ（ゲームオーバー・フィニッシュ中はtrueにする）
    // 敵を倒した直後にゲームオーバーになった場合、
    // シェイクが後から発動して揺れっぱなしになるのを防ぐ
    private bool _shakeDisabled = false;

    // ==================================================
    // LateUpdate: 毎フレーム勇者を追いかける
    // Updateより後に実行されるのでキャラの移動後に追従できる
    // ==================================================
    void LateUpdate()
    {
        if (_target == null) return;

        // シェイク中はShakeCoroutineが位置を制御するのでスキップ
        if (_isShaking) return;

        // 勇者の位置にオフセットを加えてカメラを配置
        transform.position = _target.position + _offset;
    }

    // ==================================================
    // ShakeCamera: カメラを揺らす
    // 敵を倒した時にYushaBrainから呼ぶ
    // シェイク禁止中（ゲームオーバー・フィニッシュ中）は何もしない
    // ==================================================
    public void ShakeCamera()
    {
        if (_shakeDisabled) return;

        // 既にシェイク中なら一旦止めて再度シェイク
        if (_isShaking)
            StopAllCoroutines();

        StartCoroutine(ShakeCoroutine());
    }

    // ==================================================
    // StopShake: シェイクを強制停止し、以降のシェイクも禁止する
    // ゲームオーバー・フィニッシュのタイミングで呼ぶ
    // ==================================================
    public void StopShake()
    {
        // 以降のShakeCamera()呼び出しを無視するようにする
        _shakeDisabled = true;

        if (!_isShaking) return;

        StopAllCoroutines();
        _isShaking = false;

        // 揺れを止めて通常位置に戻す
        if (_target != null)
            transform.position = _target.position + _offset;
    }

    // ==================================================
    // EnableShake: シェイク禁止を解除する
    // もう一度プレイ・タイトル復帰時に呼ぶ
    // ==================================================
    public void EnableShake()
    {
        _shakeDisabled = false;
    }

    // ==================================================
    // ShakeCoroutine: 一定時間カメラをランダムに揺らす
    // ==================================================
    IEnumerator ShakeCoroutine()
    {
        _isShaking = true;

        float elapsed = 0f;

        while (elapsed < _shakeDuration)
        {
            // ランダムな方向に揺らす
            Vector3 randomOffset = Random.insideUnitSphere * _shakeMagnitude;
            transform.position = _target.position + _offset + randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // シェイク終了後に元の位置に戻す
        transform.position = _target.position + _offset;
        _isShaking = false;
    }
}