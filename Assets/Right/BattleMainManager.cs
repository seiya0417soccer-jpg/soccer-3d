using R3;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// BattleMainManager.cs
/// パズル消去数→勇者スピードバフ計算・デバフ管理
/// 
/// - ブロック破壊数に応じて勇者にスピードバフを付与
/// - EキーBomb発動時に勇者にデバフを付与
/// - VContainerでYushaBrainを注入（SerializeField依存をなくす）
/// - BattleSettingSOでパラメーターを管理（プランナーが調整可能）
/// </summary>
public class BattleMainManager : MonoBehaviour
{

    // プランナーが調整できるパラメーターをSOで管理
    [SerializeField] private BattleSettingSO _battleSettingSO;

    // VContainerで注入される依存クラス
    private YushaBrain _yusha;

    // IPuzzleFieldを購読する（VContainerで注入される）
    private IPuzzleField _puzzleField;

    // 現在有効なバフエントリのリスト
    private List<BuffEntry> _activeBuffs = new List<BuffEntry>();

    // デバフ中フラグ
    private bool _isEKeyDebuffActive = false;
    private Coroutine _eKeyDebuffCoroutine;

    // バフの総量（activeBuffsから都度計算）
    private float TotalBonusSpeed
    {
        get
        {
            float total = 0f;
            foreach (var b in _activeBuffs) total += b.amount;
            return total;
        }
    }

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(YushaBrain yusha, IPuzzleField puzzleField)
    {
        _yusha = yusha;
        _puzzleField = puzzleField;
    }

    // ==================================================
    // Start: 初期化確認
    // ==================================================
    void Start()
    {
        if (_yusha == null)
            Debug.LogError("BattleMainManager: YushaBrainが注入されていません！");
        if (_battleSettingSO == null)
            Debug.LogError("BattleMainManager: BattleSettingSOがセットされていません！");

        // IPuzzleFieldのSubjectを購読してバフ・デバフを適用する
        _puzzleField.OnBlocksDestroyed
            .Subscribe(count => OnBlocksDestroyed(count))
            .AddTo(this);

        _puzzleField.OnEKeyBombExploded
            .Subscribe(_ => ApplyEKeyDebuff(_battleSettingSO.EKeyDebuffDuration))
            .AddTo(this);
    }

    // ==================================================
    // 通常ブロック破壊時（速度バフ付与）
    // ==================================================
    public void OnBlocksDestroyed(int destroyedCount)
    {
        float duration = destroyedCount * _battleSettingSO.SecondsPerBlock;
        float speedAmount = destroyedCount * _battleSettingSO.SpeedPerBlock;
        StartCoroutine(SpeedBuffCoroutine(duration, speedAmount));
    }

    // バフを1件追加 → 時間経過後に除去
    IEnumerator SpeedBuffCoroutine(float duration, float amount)
    {
        var entry = new BuffEntry(amount);
        _activeBuffs.Add(entry);
        ApplySpeed(); // バフ追加後に速度反映
        yield return new WaitForSeconds(duration);
        _activeBuffs.Remove(entry);
        ApplySpeed(); // バフ除去後に速度反映
    }

    // ==================================================
    // Eキー爆弾デバフ（一定時間停止）
    // ==================================================
    public void ApplyEKeyDebuff(float duration)
    {
        if (_eKeyDebuffCoroutine != null)
            StopCoroutine(_eKeyDebuffCoroutine);

        // YushaBrainのデバフも同時に適用する
        _yusha?.ApplyEKeyDebuff(_battleSettingSO.EKeyDebuffDuration);

        _eKeyDebuffCoroutine = StartCoroutine(EKeyDebuffCoroutine(_battleSettingSO.EKeyDebuffDuration));
    }

    IEnumerator EKeyDebuffCoroutine(float duration)
    {
        if (_yusha == null) yield break;

        _isEKeyDebuffActive = true;
        _yusha.UpdateSpeed(-_yusha.DefaultSpeed); // 勇者を停止
        yield return new WaitForSeconds(duration);
        _isEKeyDebuffActive = false;
        _eKeyDebuffCoroutine = null;
        ApplySpeed(); // デバフ解除後、現在のバフ量を反映
    }

    // ==================================================
    // 現在のバフ量を勇者に反映
    // デバフ中は無視（デバフ解除時にApplySpeedが呼ばれる）
    // ==================================================
    void ApplySpeed()
    {
        if (_yusha == null) return;
        if (_isEKeyDebuffActive) return; // デバフ中はバフを反映しない

        float bonusSpeed = TotalBonusSpeed;
        _yusha.UpdateSpeed(bonusSpeed);

        // バフ量に応じて発光強度を変える（最大3倍速で最大発光）
        float emissionIntensity = Mathf.Clamp01(bonusSpeed / 3f);
        _yusha.SetEmission(emissionIntensity);
    }

    // ==================================================
    // 一時停止
    // ==================================================
    public bool IsPaused { get; private set; }
    public void SetPause(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    // ==================================================
    // バフエントリ（参照で管理するためクラスを使用）
    // ==================================================
    private class BuffEntry
    {
        public float amount;
        public BuffEntry(float amount) { this.amount = amount; }
    }
}