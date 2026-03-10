using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoCenter : MonoBehaviour
{
    [Header("Board Offset")]
    public float offsetX = 100f;
    public float offsetY = 0f;

    [Header("Settings")]
    [SerializeField] private PuzzleFieldSO puzzleFieldSO;
    [SerializeField] private CameraSettingSO cameraSettingSO;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cameraSettingSO != null)
            cam.orthographicSize = cameraSettingSO.orthographicSize;

        CenterCamera();
    }

    void CenterCamera()
    {
        int width = puzzleFieldSO != null ? puzzleFieldSO.Wide : 13;
        int height = puzzleFieldSO != null ? puzzleFieldSO.Hight : 22;

        float centerX = offsetX + width / 2f;
        float bottomEdge = offsetY - 0.5f;
        float halfHeight = cam.orthographicSize;
        float centerY = bottomEdge + halfHeight;
        float z = cameraSettingSO != null ? cameraSettingSO.cameraZ : -24f;

        transform.position = new Vector3(centerX, centerY, z);
    }
}
