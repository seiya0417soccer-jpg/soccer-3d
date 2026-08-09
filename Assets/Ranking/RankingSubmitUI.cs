using Cysharp.Threading.Tasks;
using R3;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// RankingSubmitUI.cs
/// 名前入力・スコア送信UIの管理クラス
/// 
/// - プレイヤーが名前を入力してスコアをオンラインに送信する
/// - 送信完了・キャンセルをObservableで通知する
///   → GameFlowManagerを直接知らなくていい設計にした（疎結合）
///   → IPuzzleField・IBattleFieldと同じ思想で一貫している
/// - RankingSubmitStateがObservableを購読して遷移を判断する
/// - CancellationTokenでMonoBehaviour破棄時に通信を安全にキャンセルする
/// </summary>
public class RankingSubmitUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nameInputField; // 名前入力欄
    [SerializeField] private Button _submitButton;           // 送信ボタン
    [SerializeField] private Button _cancelButton;           // キャンセルボタン
    [SerializeField] private TextMeshProUGUI _statusText;    // 送信状態を表示するテキスト

    // IScoreRepositoryを通してスコアを送信する（具体型に依存しない）
    private IScoreRepository _scoreRepository;

    // IScoreReaderを通して現在のスコアを読み取る
    private IScoreReader _scoreReader;

    // ==================================================
    // 送信完了時に発火するSubject
    // 発火する権利はRankingSubmitUIだけが持ち外部にはObservableとして公開する
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
    // GameFlowManagerを受け取らない設計にした
    // → UIは通知するだけ・遷移の判断はStateが行う（責務分離）
    // ==================================================
    [Inject]
    public void Construct(
        IScoreRepository scoreRepository,
        IScoreReader scoreReader)
    {
        _scoreRepository = scoreRepository;
        _scoreReader = scoreReader;
    }

    // ==================================================
    // Start: ボタンにイベントを登録する
    // ==================================================
    void Start()
    {
        _submitButton.onClick.AddListener(OnSubmitClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);
    }

    // ==================================================
    // OnDestroy: Subjectを破棄する
    // ==================================================
    void OnDestroy()
    {
        // メモリリーク防止のためSubjectを破棄する
        _onSubmitCompleted.Dispose();
        _onCancelled.Dispose();
    }

    // ==================================================
    // OnSubmitClicked: 送信ボタン押下時の処理
    // ==================================================
    void OnSubmitClicked()
    {
        SubmitAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // ==================================================
    // SubmitAsync: スコアをAPIサーバーに送信する非同期処理
    // 送信中はボタンを無効化してStatusTextに状態を表示する
    // 送信完了後はOnSubmitCompletedを発火してStateに通知する
    // ==================================================
    private async UniTaskVoid SubmitAsync(CancellationToken ct)
    {
        string playerName = _nameInputField.text.Trim();

        // 名前が空の場合はデフォルト名を使う
        if (string.IsNullOrEmpty(playerName))
            playerName = "名無し";

        // 送信中はボタンを無効化する
        _submitButton.interactable = false;
        _cancelButton.interactable = false;
        _statusText.text = "送信中...";

        var scoreData = new PlayerScoreData(playerName, _scoreReader.Score);

        try
        {
            await _scoreRepository.SaveScoreAsync(scoreData, ct);
            _statusText.text = "送信完了！";

            // 少し待ってから送信完了を通知する
            // RankingSubmitStateがこれを受けてRankingViewStateへ遷移する
            await UniTask.Delay(500, cancellationToken: ct);
            _onSubmitCompleted.OnNext(Unit.Default);
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルは正常系として扱う（シーン遷移時等）
            return;
        }
        catch (System.Exception e)
        {
            // 送信失敗時はエラーを表示してボタンを再有効化する
            _statusText.text = "送信失敗...もう一度試してください";
            _submitButton.interactable = true;
            _cancelButton.interactable = true;
            Debug.LogError($"RankingSubmitUI: 送信失敗 → {e.Message}");
        }
    }

    // ==================================================
    // OnCancelClicked: キャンセルボタン押下時の処理
    // キャンセルを通知する
    // RankingSubmitStateがこれを受けてタイトルへ遷移する
    // ==================================================
    void OnCancelClicked()
    {
        _onCancelled.OnNext(Unit.Default);
    }

    // ==================================================
    // ResetUI: UIをリセットする
    // RankingSubmitStateのEnterから呼ぶ
    // ==================================================
    public void ResetUI()
    {
        _nameInputField.text = "";
        _statusText.text = "";
        _submitButton.interactable = true;
        _cancelButton.interactable = true;
    }
}