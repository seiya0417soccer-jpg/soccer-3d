using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// IScoreRepository.cs
/// スコアデータへのアクセスを抽象化するインターフェース
/// 
/// - LocalScoreRepository（PlayerPrefs）とApiScoreRepository（Docker）が実装する
/// - 保存先を差し替えてもRankingUI側は変更不要（拡張性の向上）
/// </summary>
public interface IScoreRepository
{
    // ランキングを取得する
    UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default);

    // スコアを保存する
    UniTask SaveScoreAsync(PlayerScoreData score, CancellationToken ct = default);
}