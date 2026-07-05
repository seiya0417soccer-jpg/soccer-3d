using UnityEngine;
using TMPro;
using VContainer;

/// <summary>
/// ResultManager.cs
/// リザルト画面の表示・操作管理
/// - 今回のキル数表示
/// - 自己ベストをPlayerPrefsで保存・表示
/// - Enter: もう一度（カウントダウンから）
/// - Backspace: タイトルへ
/// - IScoreReaderでスコード読み取り・IScoreWriterでリセット
/// </summary>
public class ResultManager : MonoBehaviour
{
    // シングルトン：他スクリプトからResultManager.Instanceでアクセス
    public static ResultManager Instance;

    [SerializeField] private GameObject resultPanel;

    // TMPコンポーネントをキャッシュしておく（毎フレームGetComponentしない）
    [SerializeField] private TextMeshProUGUI _bestScoreText;
    [SerializeField] private TextMeshProUGUI _nowScoreText;

    // リザルト画面が表示中かどうかのフラグ
    private bool _isActive = false;

    // PlayerPrefsのキー定数
    private const string BestScoreKey = "BestScore";

    // IScoreReaderで読み取り・IScoreWriterでリセット（ScoreManager直接参照をやめる）
    private IScoreReader _scoreReader;
    private IScoreWriter _scoreWriter;

    // VContainerで注入される依存クラス
    private GameFlowManager _gameFlowManager;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(ScoreManager scoreManager, GameFlowManager gameFlowManager)
    {
        // ScoreManagerをIScoreReader・IScoreWriterとして受け取る
        _scoreReader = scoreManager;
        _scoreWriter = scoreManager;
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Awake: Singleton登録
    // ==================================================
    void Awake()
    {
        // 既にインスタンスが存在する場合は自分を破棄する（重複防止）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 最初の1つだけ登録する
        Instance = this;
    }

    // ==================================================
    // Update: キー入力でリザルト操作
    // ==================================================
    void Update()
    {
        if (!_isActive) return;

        // Enterでもう一度
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            PlayAgain();

        // Backspaceでタイトルへ
        if (Input.GetKeyDown(KeyCode.Backspace))
            GoToTitle();
    }

    // ==================================================
    // リザルト表示
    // GameFlowManagerから呼ぶ
    // ==================================================
    public void ShowResult()
    {
        resultPanel.SetActive(true);
        _isActive = true;

        // IScoreReaderを通してスコアを読み取る（書き換え不可）
        int currentScore = _scoreReader.Score;

        // 自己ベスト更新
        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        _bestScoreText.text = "Best Score: " + bestScore;
        _nowScoreText.text = "Now Score: " + currentScore;
    }

    // ==================================================
    // もう一度（カウントダウンから再スタート）
    // ==================================================
    void PlayAgain()
    {
        _isActive = false;
        resultPanel.SetActive(false);

        // IScoreWriterを通してリセット（直接書き換え不可）
        _scoreWriter.ResetScore();

        _gameFlowManager.RestartFromCountdown();
    }

    // ==================================================
    // タイトルへ
    // ==================================================
    void GoToTitle()
    {
        _isActive = false;
        resultPanel.SetActive(false);

        // IScoreWriterを通してリセット（直接書き換え不可）
        _scoreWriter.ResetScore();

        _gameFlowManager.GoToTitle();
    }
}