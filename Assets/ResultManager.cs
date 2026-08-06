using UnityEngine;
using TMPro;
using VContainer;

/// <summary>
/// ResultManager.cs
/// リザルト画面の表示・操作管理
/// 
/// - 今回のキル数と自己ベストスコアを表示する
/// - 自己ベストはPlayerPrefsで端末に保存する
/// - キー入力はResultStateで管理する（Stateパターンに責務を移管）
/// - IScoreReaderでスコード読み取り・書き換えは一切できない設計にした
/// - BestScoreKeyはGameConstantsで一本管理する
///   （ResetBestScoreManagerと同じキーを使うため定数の重複を防ぐ）
/// - VContainerのInjectのみで依存解決する（Singleton不使用）
/// </summary>
public class ResultManager : MonoBehaviour
{
    [SerializeField] private GameObject _resultPanel;

    // TMPコンポーネントをキャッシュしておく（毎フレームGetComponentしない）
    [SerializeField] private TextMeshProUGUI _bestScoreText;
    [SerializeField] private TextMeshProUGUI _nowScoreText;

    // IScoreReaderで読み取りのみ（ScoreManager直接参照をやめる）
    // Interfaceで受け取ることで、将来ScoreManagerを差し替えても影響を受けない
    private IScoreReader _scoreReader;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ScoreManagerをIScoreReaderとして受け取る
    // 読み取りしか必要ないためIScoreReaderのみ注入する
    // ==================================================
    [Inject]
    public void Construct(IScoreReader scoreReader)
    {
        _scoreReader = scoreReader;
    }

    // ==================================================
    // ShowResult: リザルト画面を表示する
    // ResultStateのEnterから呼ぶ
    // ==================================================
    public void ShowResult()
    {
        _resultPanel.SetActive(true);

        // IScoreReaderを通してスコアを読み取る（書き換え不可）
        int currentScore = _scoreReader.Score;

        // 自己ベストをPlayerPrefsから取得して更新する
        int bestScore = PlayerPrefs.GetInt(GameConstants.BestScoreKey, 0);
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            // 更新があった場合のみPlayerPrefsに書き込む（無駄な書き込みを避ける）
            PlayerPrefs.SetInt(GameConstants.BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        _bestScoreText.text = "Best Score: " + bestScore;
        _nowScoreText.text = "Now Score: " + currentScore;
    }

    // ==================================================
    // HideResult: リザルト画面を非表示にする
    // ResultStateのExitから呼ぶ
    // ==================================================
    public void HideResult()
    {
        _resultPanel.SetActive(false);
    }
}