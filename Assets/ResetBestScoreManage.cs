using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ResetBestScoreManager.cs
/// タイトル画面のベストスコアリセット機能
/// 
/// - リセットボタン押下で確認パネルを表示する（誤操作防止のため2段階確認にした）
/// - DeleteButton → PlayerPrefsのベストスコアと名前を削除して確認パネルを閉じる
/// - IIEButton → 確認パネルを閉じるだけ（削除しない）
/// - BestScoreKey・PlayerNameKeyはGameConstantsで一本管理する
///   → 自己ベストをリセットする時に名前履歴も一緒にリセットする
///   → 「ゲームの記録をリセットする」という意図に沿った設計にした
/// </summary>
public class ResetBestScoreManager : MonoBehaviour
{
    [SerializeField] private GameObject _checkPanel;           // 確認パネル
    [SerializeField] private Button _bestScoreResetButton;     // リセットボタン
    [SerializeField] private Button _deleteButton;             // 削除確認ボタン（はい）
    [SerializeField] private Button _iieButton;                // キャンセルボタン（いいえ）

    // ==================================================
    // Start: ボタンにイベントを登録する
    // ボタンの参照はSerializeFieldで受け取り
    // イベント登録はAddListenerでコードで管理する
    // → OnClickイベントをInspectorで設定するより変更・引き継ぎがしやすい
    // ==================================================
    void Start()
    {
        // 確認パネルは最初非表示にする（リセットボタン押下で表示）
        _checkPanel.SetActive(false);

        _bestScoreResetButton.onClick.AddListener(OnResetButtonClicked);
        _deleteButton.onClick.AddListener(OnDeleteClicked);
        _iieButton.onClick.AddListener(OnIIEClicked);
    }

    // ==================================================
    // OnResetButtonClicked: リセットボタン押下
    // 誤操作防止のため確認パネルを表示する
    // ==================================================
    void OnResetButtonClicked()
    {
        _checkPanel.SetActive(true);
    }

    // ==================================================
    // OnDeleteClicked: 削除確認ボタン（はい）押下
    // ベストスコアと名前履歴を一緒に削除する
    // 「ゲームの記録をリセットする」という意図に沿って
    // 関連するPlayerPrefsキーを全てクリアする
    // ==================================================
    void OnDeleteClicked()
    {
        // ベストスコアを削除する
        PlayerPrefs.DeleteKey(GameConstants.BestScoreKey);

        // 名前履歴も削除する
        // 自己ベストをリセットする時は名前履歴もリセットする方が自然なため
        PlayerPrefs.DeleteKey(GameConstants.PlayerNameKey);

        PlayerPrefs.Save();
        _checkPanel.SetActive(false);
        Debug.Log("ベストスコアと名前履歴をリセットしました");
    }

    // ==================================================
    // OnIIEClicked: キャンセルボタン（いいえ）押下
    // 確認パネルを閉じるだけで削除はしない
    // ==================================================
    void OnIIEClicked()
    {
        _checkPanel.SetActive(false);
    }
}