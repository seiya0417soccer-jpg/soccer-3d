using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// FallbackScoreRepository.cs
/// API通信失敗時にLocalにフォールバックするRepositoryクラス
/// 
/// - IScoreRepositoryを実装する
/// - まずApiScoreRepositoryで通信を試みる
/// - 通信失敗時はLocalScoreRepositoryにフォールバックする
///   → Docker未起動時やオフライン時でもランキング機能が動作する
/// - RankingSubmitUI・RankingViewはこのクラスを通してデータにアクセスする
///   → 通信先の切り替えをUI側が意識しなくてよい設計にした
/// </summary>
public class FallbackScoreRepository : IScoreRepository
{
    private readonly ApiScoreRepository _apiRepository;
    private readonly LocalScoreRepository _localRepository;

    public FallbackScoreRepository(
        ApiScoreRepository apiRepository,
        LocalScoreRepository localRepository)
    {
        _apiRepository = apiRepository;
        _localRepository = localRepository;
    }

    // ==================================================
    // GetRankingAsync: ランキングを取得する
    // まずAPIから取得を試みる
    // 失敗したらLocalから取得する（フォールバック）
    // ==================================================
    public async UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _apiRepository.GetRankingAsync(ct);

            // 空リストが返った場合もフォールバックする
            if (result == null || result.Count == 0)
            {
                Debug.Log("FallbackScoreRepository: API結果が空のためLocalにフォールバック");
                return await _localRepository.GetRankingAsync(ct);
            }

            return result;
        }
        catch (System.OperationCanceledException)
        {
            throw; // キャンセルはそのまま上に伝える
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"FallbackScoreRepository: API取得失敗・Localにフォールバック → {e.Message}");
            return await _localRepository.GetRankingAsync(ct);
        }
    }

    // ==================================================
    // SaveScoreAsync: スコアを保存する
    // APIとLocalの両方に保存する
    // API失敗時はLocalだけに保存する（フォールバック）
    // ==================================================
    public async UniTask SaveScoreAsync(PlayerScoreData score, CancellationToken ct = default)
    {
        // Localには常に保存する（オフライン時のバックアップ）
        await _localRepository.SaveScoreAsync(score, ct);

        try
        {
            // APIにも送信を試みる
            await _apiRepository.SaveScoreAsync(score, ct);
            Debug.Log("FallbackScoreRepository: APIとLocalの両方に保存しました");
        }
        catch (System.OperationCanceledException)
        {
            throw; // キャンセルはそのまま上に伝える
        }
        catch (System.Exception e)
        {
            // API失敗時はLocalのみ保存済みとして続行する
            Debug.LogWarning($"FallbackScoreRepository: API保存失敗・Localのみ保存 → {e.Message}");
        }
    }
}