using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ResetBestScoreManager.cs
/// タイトル画面のベストスコアリセット機能
/// 
/// - リセットボタン押下で確認パネルを表示
/// - DeleteButton → PlayerPrefsのベストスコアを削除して確認パネルを閉じる
/// - IIEButton → 確認パネルを閉じるだけ
/// </summary>
public class ResetBestScoreManager : MonoBehaviour
{
    [SerializeField] private GameObject checkPanel;            // 確認パネル
    [SerializeField] private Button bestScoreResetButton;  // リセットボタン
    [SerializeField] private Button deleteButton;          // 削除確認ボタン（はい）
    [SerializeField] private Button iieButton;             // キャンセルボタン（いいえ）

    private const string BestScoreKey = "BestScore"; // PlayerPrefsのキー（ResultManagerと同じ）

    // ==================================================
    // Start: ボタンにイベントを登録
    // ==================================================
    void Start()
    {
        checkPanel.SetActive(false); // 確認パネルは最初非表示

        bestScoreResetButton.onClick.AddListener(OnResetButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
        iieButton.onClick.AddListener(OnIIEClicked);
    }

    // ==================================================
    // リセットボタン押下：確認パネルを表示
    // ==================================================
    void OnResetButtonClicked()
    {
        checkPanel.SetActive(true);
    }

    // ==================================================
    // DeleteButton：ベストスコアを削除して確認パネルを閉じる
    // ==================================================
    void OnDeleteClicked()
    {
        PlayerPrefs.DeleteKey(BestScoreKey);
        PlayerPrefs.Save();
        checkPanel.SetActive(false);
        Debug.Log("ベストスコアをリセットしました");
    }

    // ==================================================
    // IIEButton：確認パネルを閉じるだけ
    // ==================================================
    void OnIIEClicked()
    {
        checkPanel.SetActive(false);
    }
}
