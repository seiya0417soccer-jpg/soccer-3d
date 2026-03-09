using UnityEngine;
using TMPro;

public class TextBlinker : MonoBehaviour
{
    private TextMeshProUGUI text;
    public float speed = 2.5f;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float alpha = (Mathf.Sin(Time.unscaledTime * speed) + 1.0f) / 2.0f;
        text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
    }
}