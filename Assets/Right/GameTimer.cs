using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static float timeRemaining = 60f; // 残り時間（外部から参照可能）
    public static bool isGameOver = false;

    [SerializeField] private float totalTime = 90f; // Inspectorから変更可能
    public GameObject timerObject; // タイマー表示用テキストオブジェクト

    void Start()
    {
        timeRemaining = totalTime;
        isGameOver = false;
    }

    void Update()
    {
        if (isGameOver) return;

        // 残り時間を減らす
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isGameOver = true;
            OnTimeUp();
        }

        // テキスト更新
        string display = FormatTime(timeRemaining);

        var tmp = timerObject.GetComponent<TextMeshProUGUI>();
        var legacy = timerObject.GetComponent<UnityEngine.UI.Text>();

        if (tmp != null) tmp.text = display;
        if (legacy != null) legacy.text = display;
    }

    // 秒を "1:30" 形式にフォーマット
    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:00}", m, s);
    }

    void OnTimeUp()
    {
        // 時間切れ処理（ゲーム停止）
        Time.timeScale = 0f;
        Debug.Log("Time Up! Score: " + ScoreManager.score);
    }
}
