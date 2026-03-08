using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoCenter : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 13;
    public int height = 22;

    [Header("Board Offset")]
    public float offsetX = 100f;
    public float offsetY = 0f;

    [Header("Camera Settings")]
    public float cameraZ = 0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        CenterCamera();
    }

    void CenterCamera()
    {
        float centerX = offsetX + width / 2f;

        // ★ 修正ポイント
        // グリッドが0.5f中心配置の場合、実際の下端は -0.5f になる
        float bottomEdge = offsetY - 0.5f;

        float halfHeight = cam.orthographicSize;

        // カメラY位置 = 下端 + 半分の高さ
        float centerY = bottomEdge + halfHeight;

        transform.position = new Vector3(centerX, centerY, cameraZ);
    }
}