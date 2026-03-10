using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettingSO", menuName = "Settings/CameraSettingSO")]
public class CameraSettingSO : ScriptableObject
{
    public float orthographicSize = 13f;
    public float cameraZ = -24f;
}
