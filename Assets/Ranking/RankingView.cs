using Cysharp.Threading.Tasks;
using R3;
using System.Threading;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// RankingView.cs
/// ランキングTOP5を表示するUIクラス
/// 
/// - RankingViewModelを通してランキングを取得・表示する
///   → IScoreRepositoryを直接知らなくていい設計にした（責務分離）
/// - RankingViewModelのStateを購読して表示を切り替える
///   → Loading・Success・Errorを可視化する
/// - otameshiで検証したViewModel層をsoccer-3dに適用した
/// - エントリはPrefabを生成して表示する
/// </summary>
public class RankingView : MonoBehaviour
{
    [SerializeField] private Transform _entryContainer;   // エントリを並べる親オブジェクト
    [SerializeField] private GameObject _entryPrefab;     // 1行分のエントリPrefab
    [SerializeField] private TextMeshProUGUI _statusText; // 取得状態を表示するテキスト

    // RankingViewModelを通してランキングを取得する
    // IScoreRepositoryを直接知らなくていい設計にした
    private RankingViewModel _viewModel;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(RankingViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    // ==================================================
    // LoadAndDisplay: ランキングを取得して表示する
    // RankingViewStateのEnter経由でGameFlowManagerから呼ぶ
    // ==================================================
    public void LoadAndDisplay(CancellationToken ct)
    {
        // ViewModelのStateを購読して表示を切り替える
        // AddTo(this)でMonoBehaviour破棄時に自動で購読解除する
        _viewModel.State
            .Subscribe(state => OnStateChanged(state))
            .AddTo(this);

        // ViewModelのRankingsを購読してエントリを表示する
        _viewModel.Rankings
            .Subscribe(rankings =>
            {
                if (rankings == null) return;
                ClearEntries();
                DisplayRanking(rankings);
            })
            .AddTo(this);

        // ランキングを取得する
        _viewModel.LoadAsync(ct).Forget();
    }

    // ==================================================
    // OnStateChanged: Stateに応じて表示を切り替える
    // Loading・Success・Errorを可視化する
    // ==================================================
    private void OnStateChanged(RankingState state)
    {
        if (state is LoadingState)
        {
            _statusText.text = "取得中...";
        }
        else if (state is SuccessState)
        {
            _statusText.text = "";
        }
        else if (state is ErrorState errorState)
        {
            _statusText.text = $"取得失敗: {errorState.Message}";
        }
    }

    // ==================================================
    // DisplayRanking: ランキングをエントリとして表示する
    // ==================================================
    private void DisplayRanking(System.Collections.Generic.List<PlayerScoreData> rankings)
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