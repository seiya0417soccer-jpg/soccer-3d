using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoCenter : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 12;
    public int height = 22;

    [Header("Board Offset")]
    public float offsetX = 100f;
    public float offsetY = 0f;

    [Header("Camera Settings")]
    public float cameraZ = -40f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        CenterCamera();
    }

    void CenterCamera()
    {
        float centerX = offsetX + width / 2f;

        // 下端が0.5f見切れないようにする
        float bottomEdge = offsetY; // ボードの下端
        float halfHeight = cam.orthographicSize;

        // カメラY位置 = 下端 + 半分の高さ
        float centerY = bottomEdge + halfHeight;

        transform.position = new Vector3(centerX, centerY, cameraZ);
    }
}