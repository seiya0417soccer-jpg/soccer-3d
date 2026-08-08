using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using System.Threading;
using Unity.Loading;
using UnityEngine;

/// <summary>
/// RankingViewModel.cs
/// ランキング機能の状態管理クラス
/// 
/// - otameshiで検証したViewModel層をsoccer-3dに適用した
/// - ReactivePropertyで状態変化をViewに通知する（R3）
/// - RankingStateで「読込中・成功・失敗」を管理する
///   → UIはStateを見て表示を切り替えるだけでよい
/// - IScoreRepositoryを通してデータにアクセスする（具体型に依存しない）
/// </summary>
public class RankingViewModel
{
    // ランキングデータ（ReactivePropertyで変化を通知する）
    public ReactiveProperty<List<PlayerScoreData>> Rankings { get; }
        = new ReactiveProperty<List<PlayerScoreData>>();

    // 現在の状態（Loading・Success・Error）
    public ReactiveProperty<RankingState> State { get; }
        = new ReactiveProperty<RankingState>();

    private readonly IScoreRepository _repository;

    public RankingViewModel(IScoreRepository repository)
    {
        _repository = repository;
    }

    // ==================================================
    // LoadAsync: ランキングを取得して状態を更新する
    // RankingViewから呼ぶ
    // ==================================================
    public async UniTask LoadAsync(CancellationToken ct = default)
    {
        State.Value = new LoadingState();

        try
        {
            var result = await _repository.GetRankingAsync(ct);
            Rankings.Value = result;
            State.Value = new SuccessState();
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルは正常系として扱う
            Debug.Log("RankingViewModel: キャンセルされました");
        }
        catch (System.Exception e)
        {
            State.Value = new ErrorState($"取得失敗: {e.Message}");
            Debug.LogError($"RankingViewModel: 取得失敗 → {e.Message}");
        }
    }

    // ==================================================
    // SubmitAsync: スコアを送信して状態を更新する
    // RankingSubmitUIから呼ぶ
    // ==================================================
    public async UniTask SubmitAsync(PlayerScoreData score, CancellationToken ct = default)
    {
        State.Value = new LoadingState();

        try
        {
            await _repository.SaveScoreAsync(score, ct);
            State.Value = new SuccessState();
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("RankingViewModel: キャンセルされました");
        }
        catch (System.Exception e)
        {
            State.Value = new ErrorState($"送信失敗: {e.Message}");
            Debug.LogError($"RankingViewModel: 送信失敗 → {e.Message}");
        }
    }
}