using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static float timeRemaining = 60f;
    public static bool isGameOver = false;

    [SerializeField] private float totalTime = 90f;
    public GameObject timerObject;

    private bool isRunning = false; // StartTimer()が呼ばれるまで動かない

    void Start()
    {
        timeRemaining = totalTime;
        isGameOver = false;
        isRunning = false;
    }

    // GameFlowManagerから呼ばれる
    public void StartTimer()
    {
        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;
        if (isGameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isGameOver = true;
            isRunning = false;
            OnTimeUp();
        }

        // テキスト更新
        string display = FormatTime(timeRemaining);
        var tmp = timerObject.GetComponent<TextMeshProUGUI>();
        var legacy = timerObject.GetComponent<UnityEngine.UI.Text>();
        if (tmp != null) tmp.text = display;
        if (legacy != null) legacy.text = display;
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:00}", m, s);
    }

    void OnTimeUp()
    {
        Debug.Log("Time Up!");
        // GameFlowManagerにフィニッシュを通知
        GameFlowManager.Instance?.OnFinish();
    }
}
