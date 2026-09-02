using Cysharp.Threading.Tasks;
using R3;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// RankingSubmitUI.cs
/// 名前入力・スコア送信UIの管理クラス
/// 
/// - プレイヤーが名前を入力してスコアをオンラインに送信する
/// - RankingViewModelを通してスコアを送信する
///   → IScoreRepositoryを直接知らなくていい設計にした（責務分離）
/// - ViewModelのStateを購読して送信中・成功・失敗を可視化する
/// - 送信完了・キャンセルをObservableで通知する
///   → GameFlowManagerを直接知らなくていい設計にした（疎結合）
/// - 一度入力した名前をPlayerPrefsに保存して次回以降自動入力する
///   → 毎回名前を入力する手間を省いてゲームのテンポを守る
/// - otameshiで検証したViewModelをsoccer-3dに適用した
/// </summary>
public class RankingSubmitUI : MonoBehaviour
{
    [SerializeField] private InputField _nameInputField; // 名前入力欄
    [SerializeField] private Button _submitButton;       // 送信ボタン
    [SerializeField] private Button _cancelButton;       // キャンセルボタン
    [SerializeField] private Text _statusText;           // 送信状態を表示するテキスト

    // PlayerPrefsのキー定数（名前を保存・再利用する）
    private const string PlayerNameKey = "PlayerName";

    // RankingViewModelを通してスコアを送信する（IScoreRepositoryを直接知らない）
    private RankingViewModel _viewModel;

    // IScoreReaderを通して現在のスコアを読み取る
    private IScoreReader _scoreReader;

    // ==================================================
    // 送信完了時に発火するSubject
    // RankingSubmitStateがこれを購読してRankingViewStateへ遷移する
    // ==================================================
    private readonly Subject<Unit> _onSubmitCompleted = new Subject<Unit>();
    public Observable<Unit> OnSubmitCompleted => _onSubmitCompleted;

    // ==================================================
    // キャンセル時に発火するSubject
    // RankingSubmitStateがこれを購読してタイトルへ遷移する
    // ==================================================
    private readonly Subject<Unit> _onCancelled = new Subject<Unit>();
    public Observable<Unit> OnCancelled => _onCancelled;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // RankingViewModelを受け取る（IScoreRepositoryは受け取らない）
    // ==================================================
    [Inject]
    public void Construct(
        RankingViewModel viewModel,
        IScoreReader scoreReader)
    {
        _viewModel = viewModel;
        _scoreReader = scoreReader;
    }

    // ==================================================
    // Start: ボタンにイベントを登録する
    // 前回入力した名前をPlayerPrefsから取得してInputFieldに設定する
    // ViewModelのStateを購読して表示を切り替える
    // ==================================================
    void Start()
    {
        _submitButton.onClick.AddListener(OnSubmitClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);

        // 前回入力した名前をPlayerPrefsから取得してInputFieldに設定する
        // 初回は空文字なので何も表示されない
        _nameInputField.text = PlayerPrefs.GetString(PlayerNameKey, "");

        // ViewModelのStateを購読して送信状態を可視化する
        // AddTo(this)でMonoBehaviour破棄時に自動で購読解除する（メモリリーク防止）
        _viewModel.State
            .Subscribe(state => OnStateChanged(state))
            .AddTo(this);
    }

    // ==================================================
    // OnDestroy: Subjectを破棄する
    // ==================================================
    void OnDestroy()
    {
        _onSubmitCompleted.Dispose();
        _onCancelled.Dispose();
    }

    // ==================================================
    // OnStateChanged: Stateに応じて表示を切り替える
    // Loading・Success・Errorを可視化する
    // ==================================================
    private void OnStateChanged(RankingState state)
    {
        if (state is LoadingState)
        {
            _statusText.text = "送信中...";
            _submitButton.interactable = false;
            _cancelButton.interactable = false;
        }
        else if (state is SuccessState)
        {
            _statusText.text = "送信完了！";
            // 送信完了を通知する
            _onSubmitCompleted.OnNext(Unit.Default);
        }
        else if (state is ErrorState errorState)
        {
            _statusText.text = $"送信失敗: {errorState.Message}";
            _submitButton.interactable = true;
            _cancelButton.interactable = true;
        }
    }

    // ==================================================
    // OnSubmitClicked: 送信ボタン押下時の処理
    // ==================================================
    void OnSubmitClicked()
    {
        SubmitAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // ==================================================
    // SubmitAsync: ViewModelを通してスコアを送信する
    // 送信成功時に名前をPlayerPrefsに保存して次回以降自動入力する
    // 送信処理・エラーハンドリングはViewModelが担当する
    // ==================================================
    private async UniTaskVoid SubmitAsync(CancellationToken ct)
    {
        string playerName = _nameInputField.text.Trim();

        // 名前が空の場合はデフォルト名を使う
        if (string.IsNullOrEmpty(playerName))
            playerName = "名無し";

        var scoreData = new PlayerScoreData(playerName, _scoreReader.Score);

        // ViewModelを通して送信する
        // 状態管理・エラーハンドリングはViewModelが担当する
        await _viewModel.SubmitAsync(scoreData, ct);

        // 送信後にStateを確認して名前を保存する
        // ErrorStateでなければ成功とみなして保存する
        if (_viewModel.State.Value is not ErrorState)
        {
            PlayerPrefs.SetString(PlayerNameKey, playerName);
            PlayerPrefs.Save();
        }
    }

    // ==================================================
    // OnCancelClicked: キャンセルボタン押下時の処理
    // キャンセルを通知する
    // ==================================================
    void OnCancelClicked()
    {
        _onCancelled.OnNext(Unit.Default);
    }

    // ==================================================
    // ResetUI: UIをリセットする
    // RankingSubmitStateのEnterから呼ぶ
    // 名前はリセットしない（前回の名前を残す）
    // ==================================================
    public void ResetUI()
    {
        // 名前は前回の入力を引き継ぐためリセットしない
        _statusText.text = "";
        _submitButton.interactable = true;
        _cancelButton.interactable = true;
    }
}