using Cysharp.Threading.Tasks;
using R3;
using System;
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
///   → 購読はResetUI()で行い、前回の購読を解除してから再購読する
///   → RankingViewModelがSingletonのため前回のStateが残らないようにした
/// - 送信完了・キャンセルをObservableで通知する
///   → GameFlowManagerを直接知らなくていい設計にした（疎結合）
/// - 通信失敗時は最大3回までユーザーが再送できる
///   → 3回失敗したらENTERテキストを表示してリザルトへ戻る
/// - 一度入力した名前をGameConstants.PlayerNameKeyで保存して次回以降自動入力する
///   → 定数をGameConstantsで一本管理することで修正漏れを防ぐ
///   → 毎回名前を入力する手間を省いてゲームのテンポを守る
/// - otameshiで検証したViewModelをsoccer-3dに適用した
/// </summary>
public class RankingSubmitUI : MonoBehaviour
{
    [SerializeField] private InputField _nameInputField;     // 名前入力欄
    [SerializeField] private Button _submitButton;           // 送信ボタン
    [SerializeField] private Button _cancelButton;           // キャンセルボタン
    [SerializeField] private Text _statusText;               // 送信状態を表示するテキスト
    [SerializeField] private Text _submitButtonText;         // 送信ボタンのテキスト
    [SerializeField] private GameObject _pushEnterText;      // 3回失敗時に表示するENTERテキスト
    [SerializeField] private Button _clearNameButton;        // 名前削除ボタン

    // 最大リトライ回数
    private const int MaxRetryCount = 3;

    // 現在の送信試行回数
    private int _retryCount = 0;

    // RankingViewModelを通してスコアを送信する（IScoreRepositoryを直接知らない）
    private RankingViewModel _viewModel;

    // IScoreReaderを通して現在のスコアを読み取る
    private IScoreReader _scoreReader;

    // State購読のDisposable
    // ResetUI()で前回の購読を解除して再購読する
    private IDisposable _stateDisposable;

    // ==================================================
    // 送信完了時に発火するSubject
    // RankingSubmitStateがこれを購読してRankingViewStateへ遷移する
    // 発火する権利はRankingSubmitUIだけが持ち外部にはObservableとして公開する
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
    // 3回失敗時に発火するSubject
    // RankingSubmitStateがこれを購読してEnter待ちに移行する
    // ==================================================
    private readonly Subject<Unit> _onMaxRetryReached = new Subject<Unit>();
    public Observable<Unit> OnMaxRetryReached => _onMaxRetryReached;

    // ==================================================
    // Inject: VContainerから依存を注入される
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
    // GameConstants.PlayerNameKeyで定数を一本管理する
    // State購読はResetUI()で行うためここでは行わない
    // ==================================================
    void Start()
    {
        _submitButton.onClick.AddListener(OnSubmitClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);
        _clearNameButton.onClick.AddListener(ClearName);

        // 前回入力した名前をPlayerPrefsから取得してInputFieldに設定する
        // 初回は空文字なので何も表示されない
        _nameInputField.text = PlayerPrefs.GetString(GameConstants.PlayerNameKey, "");
    }

    // ==================================================
    // OnDestroy: SubjectとDisposableを破棄する
    // ==================================================
    void OnDestroy()
    {
        _onSubmitCompleted.Dispose();
        _onCancelled.Dispose();
        _onMaxRetryReached.Dispose();
        _stateDisposable?.Dispose();
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
            _onSubmitCompleted.OnNext(Unit.Default);
        }
        else if (state is ErrorState)
        {
            _retryCount++;

            if (_retryCount < MaxRetryCount)
            {
                // まだリトライできる
                _statusText.text = $"通信に失敗しました（{_retryCount}/{MaxRetryCount}）";
                _submitButtonText.text = "もう一度送信";
                _submitButton.interactable = true;
                _cancelButton.interactable = true;
            }
            else
            {
                // 最大リトライ回数に達した
                _statusText.text = "通信できないため自己ベストとして記録します";
                _submitButton.gameObject.SetActive(false);
                _cancelButton.gameObject.SetActive(false);
                _pushEnterText.SetActive(true);

                // RankingSubmitStateに通知してEnter待ちに移行する
                _onMaxRetryReached.OnNext(Unit.Default);
            }
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
    // 送信後にStateを確認して名前を保存する
    // GameConstants.PlayerNameKeyで定数を一本管理する
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

        // ErrorStateでなければ成功とみなして名前を保存する
        if (_viewModel.State.Value is not ErrorState)
        {
            PlayerPrefs.SetString(GameConstants.PlayerNameKey, playerName);
            PlayerPrefs.Save();
        }
    }

    // ==================================================
    // OnCancelClicked: キャンセルボタン押下時の処理
    // ==================================================
    void OnCancelClicked()
    {
        _onCancelled.OnNext(Unit.Default);
    }

    // ==================================================
    // ClearName: 名前入力欄をクリアする
    // 名前クリアボタンから呼ぶ
    // InputFieldとPlayerPrefsの両方をクリアする
    // ==================================================
    public void ClearName()
    {
        _nameInputField.text = "";
        PlayerPrefs.DeleteKey(GameConstants.PlayerNameKey);
        PlayerPrefs.Save();
    }

    // ==================================================
    // ResetUI: UIをリセットする
    // RankingSubmitStateのEnterから呼ぶ
    // 名前はリセットしない（前回の名前を残す）
    // State購読をここで行うことでSingletonのViewModelの
    // 前回のStateが残らないようにする（疎結合を維持）
    // ==================================================
    public void ResetUI()
    {
        _retryCount = 0;
        _statusText.text = "";
        _submitButtonText.text = "送信";
        _submitButton.gameObject.SetActive(true);
        _cancelButton.gameObject.SetActive(true);
        _submitButton.interactable = true;
        _cancelButton.interactable = true;
        _pushEnterText.SetActive(false);

        // 前回の購読を解除してから再購読する
        // RankingViewModelがSingletonのため前回のStateが残る可能性があるため
        _stateDisposable?.Dispose();
        _stateDisposable = _viewModel.State
            .Subscribe(state => OnStateChanged(state))
            .AddTo(this);
    }
}