using UnityEngine;

public class SandTexture : MonoBehaviour
{
    [Header("Texture Size")]
    [SerializeField] private int textureSize = 512;

    [Header("Noise Scale")]
    [SerializeField] private float noiseScale = 8f;

    [Header("Sand Colors")]
    [SerializeField] private Color colorLight = new Color(0.98f, 0.90f, 0.72f); // メインの砂色（明るい）
    [SerializeField] private Color colorMid = new Color(0.95f, 0.85f, 0.65f); // ほぼ同じ砂色
    [SerializeField] private Color colorDark = new Color(0.90f, 0.78f, 0.56f); // 少し暗い砂色

    [Header("Noise Seed")]
    [SerializeField] private float seedX = 0f;
    [SerializeField] private float seedY = 0f;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;
        Texture2D texture = GenerateSandTexture();
        rend.material.mainTexture = texture;
    }

    Texture2D GenerateSandTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nx = (float)x / textureSize * noiseScale + seedX;
                float ny = (float)y / textureSize * noiseScale + seedY;
                float noise = Mathf.PerlinNoise(nx, ny);

                // 3色の差を小さくしてメインの砂色が支配的になるようにブレンド
                Color color = Color.Lerp(colorDark, colorLight, noise);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}
