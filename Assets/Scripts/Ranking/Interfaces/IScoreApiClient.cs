using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// IScoreApiClient.cs
/// スコアAPIとの通信を抽象化するインターフェース
/// 
/// - DockerScoreApiClient（本番）とFakeScoreApiClient（モック）が実装する
/// - CancellationTokenを渡すことでシーン遷移時に通信を安全にキャンセルできる
/// - 実装を差し替えるだけで通信先を変えられる（拡張性の向上）
/// </summary>
public interface IScoreApiClient
{
    // ランキング上位を取得する（GET /ranking）
    UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default);

    // スコアを登録する（POST /ranking/save）
    UniTask PostScoreAsync(PlayerScoreData score, CancellationToken ct = default);
}