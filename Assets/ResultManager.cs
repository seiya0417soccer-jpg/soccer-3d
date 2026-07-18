using UnityEngine;
using TMPro;
using VContainer;

/// <summary>
/// ResultManager.cs
/// リザルト画面の表示・操作管理
/// - 今回のキル数表示
/// - 自己ベストをPlayerPrefsで保存・表示
/// - キー入力はResultStateで管理する（Stateパターンに移管）
/// - IScoreReaderでスコア読み取り・IScoreWriterでリセット
/// </summary>
public class ResultManager : MonoBehaviour
{
    // シングルトン：他スクリプトからResultManager.Instanceでアクセス
    public static ResultManager Instance;

    [SerializeField] private GameObject resultPanel;

    // TMPコンポーネントをキャッシュしておく（毎フレームGetComponentしない）
    [SerializeField] private TextMeshProUGUI _bestScoreText;
    [SerializeField] private TextMeshProUGUI _nowScoreText;

    // PlayerPrefsのキー定数
    private const string BestScoreKey = "BestScore";

    // IScoreReaderで読み取り・IScoreWriterでリセット（ScoreManager直接参照をやめる）
    private IScoreReader _scoreReader;
    private IScoreWriter _scoreWriter;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(ScoreManager scoreManager)
    {
        _scoreReader = scoreManager;
        _scoreWriter = scoreManager;
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
    // リザルト表示
    // ResultStateのEnterから呼ぶ
    // ==================================================
    public void ShowResult()
    {
        resultPanel.SetActive(true);

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
    // リザルト非表示
    // ResultStateのExitから呼ぶ
    // ==================================================
    public void HideResult()
    {
        resultPanel.SetActive(false);
    }

    // ==================================================
    // スコアリセット
    // RestartFromCountdown・GoToTitleから呼ぶ
    // ==================================================
    public void ResetScore()
    {
        // IScoreWriterを通してリセット（直接書き換え不可）
        _scoreWriter.ResetScore();
    }
}