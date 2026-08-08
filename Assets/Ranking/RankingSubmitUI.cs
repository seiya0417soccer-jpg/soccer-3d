using Cysharp.Threading.Tasks;
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
/// - 送信完了後はGameFlowManager経由でRankingViewStateへ遷移する
/// - キャンセル時はタイトルへ戻る
/// - CancellationTokenでGameFlowManager破棄時に通信を安全にキャンセルする
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

    // GameFlowManagerへの参照（State遷移に使う）
    private GameFlowManager _gameFlowManager;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(
        ApiScoreRepository scoreRepository,
        IScoreReader scoreReader,
        GameFlowManager gameFlowManager)
    {
        _scoreRepository = scoreRepository;
        _scoreReader = scoreReader;
        _gameFlowManager = gameFlowManager;
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
    // OnSubmitClicked: 送信ボタン押下時の処理
    // ==================================================
    void OnSubmitClicked()
    {
        SubmitAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // ==================================================
    // SubmitAsync: スコアをAPIサーバーに送信する非同期処理
    // 送信中はボタンを無効化してStatusTextに状態を表示する
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

            // 少し待ってからランキング表示画面へ遷移する
            await UniTask.Delay(500, cancellationToken: ct);
            _gameFlowManager.GoToRankingView();
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
    // タイトルへ戻る
    // ==================================================
    void OnCancelClicked()
    {
        _gameFlowManager.GoToTitle();
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