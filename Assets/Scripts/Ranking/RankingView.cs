using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// RankingView.cs
/// ランキングTOP5を表示するUIクラス
/// 
/// - RankingViewModelを通してランキングを取得・表示する
///   → IScoreRepositoryを直接知らなくていい設計にした（責務分離）
/// - RankingViewModelのStateを購読して表示を切り替える
///   → Loading・Success・Errorを可視化する
/// - LoadAndDisplay()を呼ぶたびに前回の購読を解除して再購読する
///   → RankingViewはパネル非表示のままMonoBehaviourとして生存し続けるため
///     何度もランキング画面を開くと購読が累積するライフサイクル上の問題があった
/// - 購読の管理はDisposableで明示的に行う
///   → AddTo(this)と併用すると責務が二重になるため使わず、
///     OnDestroy()での明示Disposeに一本化した
/// - otameshiで検証したViewModel層をsoccer-3dに適用した
/// - エントリはPrefabを生成して表示する
/// </summary>
public class RankingView : MonoBehaviour
{
    [SerializeField] private Transform _entryContainer;  // エントリを並べる親オブジェクト
    [SerializeField] private GameObject _entryPrefab;    // 1行分のエントリPrefab
    [SerializeField] private Text _statusText;           // 取得状態を表示するテキスト

    // RankingViewModelを通してランキングを取得する
    // IScoreRepositoryを直接知らなくていい設計にした
    private RankingViewModel _viewModel;

    // State・Rankings購読のDisposable
    // LoadAndDisplay()呼び出しごとに前回の購読を解除して再購読する
    // OnDestroy()で明示的に破棄する（AddTo(this)は使わず一本化）
    private IDisposable _stateDisposable;
    private IDisposable _rankingsDisposable;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(RankingViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    // ==================================================
    // OnDestroy: 購読を明示的に破棄する
    // ==================================================
    void OnDestroy()
    {
        _stateDisposable?.Dispose();
        _rankingsDisposable?.Dispose();
    }

    // ==================================================
    // LoadAndDisplay: ランキングを取得して表示する
    // RankingViewStateのEnter経由でGameFlowManagerから呼ぶ
    // 
    // RankingViewはパネル非表示のまま破棄されずに残るため
    // 呼ばれるたびに前回の購読を解除してから再購読する
    // （解除しないと画面を開くたびに購読が積み重なってしまう）
    // ==================================================
    public void LoadAndDisplay(CancellationToken ct)
    {
        // 前回の購読を解除してから再購読する
        _stateDisposable?.Dispose();
        _rankingsDisposable?.Dispose();

        // ViewModelのStateを購読して表示を切り替える
        _stateDisposable = _viewModel.State
            .Subscribe(state => OnStateChanged(state));

        // ViewModelのRankingsを購読してエントリを表示する
        _rankingsDisposable = _viewModel.Rankings
            .Subscribe(rankings =>
            {
                if (rankings == null) return;
                ClearEntries();
                DisplayRanking(rankings);
            });

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
    // TOP5のみ表示する（それ以上は切り捨てる）
    // ==================================================
    private void DisplayRanking(System.Collections.Generic.List<PlayerScoreData> rankings)
    {
        // TOP5だけ表示する
        int count = Mathf.Min(rankings.Count, 5);
        for (int i = 0; i < count; i++)
        {
            var entry = Instantiate(_entryPrefab, _entryContainer);
            var text = entry.GetComponent<Text>();
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