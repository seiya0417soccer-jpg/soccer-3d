using System.Collections;
using UnityEngine;

/// <summary>
/// CountdownState.cs
/// カウントダウン画面の状態
/// 
/// - Enter：カウントダウンを開始する
/// - Update：カウントダウン中は何もしない（Coroutineで管理）
/// - Exit：カウントダウン画面を非表示
/// </summary>
public class CountdownState : IGameState
{
    // GameFlowManagerへの参照（パネル操作・状態遷移に使う）
    private readonly GameFlowManager _gameFlowManager;

    public CountdownState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: カウントダウン開始
    // ==================================================
    public void Enter()
    {
        _gameFlowManager.ShowReadyGoPanel(true);
        _gameFlowManager.StartCountdown();
    }

    // ==================================================
    // Update: カウントダウン中は入力を受け付けない
    // ==================================================
    public void Update()
    {
        // カウントダウンはCoroutineで管理するため何もしない
    }

    // ==================================================
    // Exit: カウントダウン画面を非表示
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowReadyGoPanel(false);
    }
}