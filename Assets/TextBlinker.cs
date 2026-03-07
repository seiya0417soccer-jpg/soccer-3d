using UnityEngine;
using TMPro; // TextMeshProを使っている場合

public class TextBlinker : MonoBehaviour
{
    private TextMeshProUGUI text;
    public float speed = 2.0f; // 点滅スピード

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // サイン波を使ってアルファ値（透明度）を0〜1で揺らす
        float alpha = (Mathf.Sin(Time.time * speed) + 1.0f) / 2.0f;
        text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
    }
}