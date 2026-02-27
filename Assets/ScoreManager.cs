using UnityEngine;
using TMPro; // TMP用

public class ScoreManager : MonoBehaviour
{
    public static int score = 0;
    // 「GameObject」にすれば、どんな文字オブジェクトでもドラッグできるぜ！
    public GameObject scoreObject;

    void Update()
    {
        // 中身のテキストを強引に書き換える
        var tmp = scoreObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = "KILLS: " + score;

        var legacy = scoreObject.GetComponent<UnityEngine.UI.Text>();
        if (legacy != null) legacy.text = "KILLS: " + score;
    }
}