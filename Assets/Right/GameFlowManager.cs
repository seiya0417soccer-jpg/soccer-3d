using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameFlowManager.cs
/// タイトル → 操作説明 → カウントダウン → ゲーム開始 のフローを管理
/// カウントダウン中は Time.timeScale = 0 で全て停止
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject manualPanel;
    [SerializeField] private GameObject readyGoGroup;

    [Header("ReadyGo")]
    [SerializeField] private TextMeshProUGUI countdownText; // ReadyGoGroup内のテキスト

    [Header("References")]
    [SerializeField] private GameTimer gameTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // ゲーム開始時は全て止めてタイトルを表示
        Time.timeScale = 0f;
        titlePanel.SetActive(true);
        manualPanel.SetActive(false);
        readyGoGroup.SetActive(false);
    }

    // ==================================================
    // Update：エンターキーで画面遷移
    // ==================================================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (titlePanel.activeSelf)
                OnTitleStart();
            else if (manualPanel.activeSelf)
                OnManualNext();
        }
    }

    // ==================================================
    // タイトル画面：エンターキーで進む
    // ==================================================
    public void OnTitleStart()
    {
        titlePanel.SetActive(false);
        manualPanel.SetActive(true);
    }

    // ==================================================
    // 操作説明画面：次へボタンから呼ぶ
    // ==================================================
    public void OnManualNext()
    {
        manualPanel.SetActive(false);
        readyGoGroup.SetActive(true);
        StartCoroutine(CountdownCoroutine());
    }

    // ==================================================
    // カウントダウン → GO! → ゲーム開始
    // ==================================================
    IEnumerator CountdownCoroutine()
    {
        // カウントダウン中はUnscaledTimeを使う（timeScale=0でも動く）
        countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);

        countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);

        countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);

        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.8f);

        // ゲーム開始
        readyGoGroup.SetActive(false);
        Time.timeScale = 1f;

        // タイマー開始
        if (gameTimer != null)
            gameTimer.StartTimer();
    }
    // ==================================================
    // フィニッシュ（時間切れ）
    // ==================================================
    public void OnFinish()
    {
        Time.timeScale = 0f;
        Debug.Log("Finish!");
        // TODO: フィニッシュ画面を表示
    }
    public void OnGameOver()
    {
        Time.timeScale = 0f;
        Debug.Log("Game Over!");
        // TODO: ゲームオーバー画面を表示
    }
}
