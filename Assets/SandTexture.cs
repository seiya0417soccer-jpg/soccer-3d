using UnityEngine;

/// <summary>
/// SandTexture.cs
/// Perlinノイズを使って自然な砂模様のテクスチャを生成し
/// 既存マテリアルに貼り付ける
/// 
/// - Sandオブジェクトにアタッチするだけで動作
/// - TransformやNavMesh設定は一切変更しない
/// - 3色の砂色をノイズで自然にブレンド
/// </summary>
public class SandTexture : MonoBehaviour
{
    [Header("Texture Size")]
    [SerializeField] private int textureSize = 512;

    [Header("Noise Scale")]
    [SerializeField] private float noiseScale = 8f;

    [Header("Sand Colors")]
    [SerializeField] private Color colorLight = new Color(0.78f, 0.66f, 0.43f);
    [SerializeField] private Color colorMid = new Color(0.72f, 0.56f, 0.42f);
    [SerializeField] private Color colorDark = new Color(0.63f, 0.47f, 0.31f);

    [Header("Noise Seed")]
    [SerializeField] private float seedX = 0f;
    [SerializeField] private float seedY = 0f;
    void Start()
    {
        // ランダムシードを設定（毎回違う模様にする場合はtrueに）
        // seedX = Random.Range(0f, 100f);
        // seedY = Random.Range(0f, 100f);

        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        // Perlinノイズでテクスチャを生成
        Texture2D texture = GenerateSandTexture();

        // 既存マテリアルのメインテクスチャとして設定
        rend.material.mainTexture = texture;
    }

    // ==================================================
    // Perlinノイズで砂テクスチャを生成
    // ノイズ値に応じて3色をブレンド
    // ==================================================
    Texture2D GenerateSandTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Perlinノイズ値を取得（0〜1）
                float nx = (float)x / textureSize * noiseScale + seedX;
                float ny = (float)y / textureSize * noiseScale + seedY;
                float noise = Mathf.PerlinNoise(nx, ny);

                // ノイズ値に応じて3色をブレンド
                Color color;
                if (noise < 0.4f)
                {
                    // 暗い砂〜中間の砂
                    float t = noise / 0.4f;
                    color = Color.Lerp(colorDark, colorMid, t);
                }
                else if (noise < 0.7f)
                {
                    // 中間の砂〜明るい砂
                    float t = (noise - 0.4f) / 0.3f;
                    color = Color.Lerp(colorMid, colorLight, t);
                }
                else
                {
                    // 明るい砂（ハイライト部分）
                    float t = (noise - 0.7f) / 0.3f;
                    color = Color.Lerp(colorLight, colorLight * 1.1f, t);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}
