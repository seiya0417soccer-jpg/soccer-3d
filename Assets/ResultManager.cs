using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ResultManager.cs
/// リザルト画面の表示・操作管理
/// - 今回のキル数表示
/// - 自己ベストをPlayerPrefsで保存・表示
/// - Enter: もう一度（カウントダウンから）
/// - Backspace: タイトルへ
/// </summary>
public class ResultManager : MonoBehaviour
{
    public static ResultManager Instance;

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text nowScoreText;

    private bool isActive = false;

    private const string BestScoreKey = "BestScore";

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

    void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            PlayAgain();

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
        isActive = true;

        int currentScore = ScoreManager.score;

        // 自己ベスト更新
        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        bestScoreText.text = "自己ベスト: " + bestScore;
        nowScoreText.text = "今回の記録: " + currentScore;
    }

    // ==================================================
    // もう一度（カウントダウンから再スタート）
    // ==================================================
    void PlayAgain()
    {
        isActive = false;
        resultPanel.SetActive(false);
        ScoreManager.score = 0;
        GameFlowManager.Instance.RestartFromCountdown();
    }

    // ==================================================
    // タイトルへ
    // ==================================================
    void GoToTitle()
    {
        isActive = false;
        resultPanel.SetActive(false);
        ScoreManager.score = 0;
        GameFlowManager.Instance.GoToTitle();
    }
}
