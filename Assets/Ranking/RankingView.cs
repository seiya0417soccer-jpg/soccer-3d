using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// RankingView.cs
/// ランキングTOP5を表示するUIクラス
/// 
/// - IScoreRepositoryを通してランキングを取得する（具体型に依存しない）
/// - LoadAndDisplay()でランキングを取得して表示する
/// - エントリはPrefabを生成して表示する
/// </summary>
public class RankingView : MonoBehaviour
{
    [SerializeField] private Transform _entryContainer;  // エントリを並べる親オブジェクト
    [SerializeField] private GameObject _entryPrefab;    // 1行分のエントリPrefab
    [SerializeField] private TextMeshProUGUI _statusText; // 取得状態を表示するテキスト

    // IScoreRepository経由でランキングを取得する（具体型に依存しない）
    private IScoreRepository _scoreRepository;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(ApiScoreRepository scoreRepository)
    {
        _scoreRepository = scoreRepository;
    }

    // ==================================================
    // LoadAndDisplay: ランキングを取得して表示する
    // RankingViewStateのEnter経由でGameFlowManagerから呼ぶ
    // ==================================================
    public void LoadAndDisplay(CancellationToken ct)
    {
        LoadAsync(ct).Forget();
    }

    // ==================================================
    // LoadAsync: ランキングを非同期で取得して表示する
    // 取得中はStatusTextに状態を表示する
    // ==================================================
    private async UniTaskVoid LoadAsync(CancellationToken ct)
    {
        _statusText.text = "取得中...";
        ClearEntries();

        try
        {
            List<PlayerScoreData> rankings = await _scoreRepository.GetRankingAsync(ct);
            _statusText.text = "";
            DisplayRanking(rankings);
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルは正常系として扱う
            return;
        }
        catch (System.Exception e)
        {
            _statusText.text = "取得失敗";
            Debug.LogError($"RankingView: 取得失敗 → {e.Message}");
        }
    }

    // ==================================================
    // DisplayRanking: ランキングをエントリとして表示する
    // ==================================================
    private void DisplayRanking(List<PlayerScoreData> rankings)
    {
        for (int i = 0; i < rankings.Count; i++)
        {
            var entry = Instantiate(_entryPrefab, _entryContainer);
            var text = entry.GetComponent<TextMeshProUGUI>();
            text.text = $"#{i + 1}  {rankings[i].Name}  {rankings[i].Score} kills";
        }
    }

    // ==================================================
    // ClearEntries: 既存のエントリを全て削除する
    // 再取得時に古いエントリが残らないようにする
    // ==================================================
    private void ClearEntries()
    {
        foreach (Transform child in _entryContainer)
            Destroy(child.gameObject);
    }
}