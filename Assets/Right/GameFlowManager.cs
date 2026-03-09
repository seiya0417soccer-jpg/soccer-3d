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
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject finishPanel;

    [Header("ReadyGo")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("References")]
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private GameObject killCountObject;
    [SerializeField] private GameObject timerTextObject;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 0f;
        titlePanel.SetActive(true);
        manualPanel.SetActive(false);
        readyGoGroup.SetActive(false);
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);
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
    // 操作説明画面：エンターキーで進む
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
        countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.8f);

        readyGoGroup.SetActive(false);
        Time.timeScale = 1f;

        if (gameTimer != null)
            gameTimer.StartTimer();
    }

    // ==================================================
    // フィニッシュ（時間切れ）
    // エンターキーのみでリザルトへ
    // ==================================================
    public void OnFinish()
    {
        Time.timeScale = 0f;
        killCountObject?.SetActive(false);
        timerTextObject?.SetActive(false);
        finishPanel.SetActive(true);
        StartCoroutine(WaitForFinishInput());
    }

    IEnumerator WaitForFinishInput()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GoToResult();
                yield break;
            }
            yield return null;
        }
    }

    // ==================================================
    // ゲームオーバー
    // 3秒経過 または エンターキーでリザルトへ
    // ==================================================
    public void OnGameOver()
    {
        Time.timeScale = 0f;
        killCountObject?.SetActive(false);
        timerTextObject?.SetActive(false);
        gameOverPanel.SetActive(true);
        StartCoroutine(WaitForGameOverTransition());
    }

    IEnumerator WaitForGameOverTransition()
    {
        float elapsed = 0f;
        while (elapsed < 3f)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GoToResult();
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        GoToResult();
    }

    // ==================================================
    // リザルトへ遷移
    // ==================================================
    void GoToResult()
    {
        Debug.Log("Go to Result!");
        // TODO: リザルト画面を表示
    }
}
