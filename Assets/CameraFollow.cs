using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // ’Ç‚¢‚©‚¯‚é—EÒ
    public Vector3 offset = new Vector3(0, 15, -10); // —EÒ‚©‚ç‚Ì‹——£

    void LateUpdate()
    {
        if (target != null)
        {
            // —EÒ‚ÌˆÊ’u { Œˆ‚Ü‚Á‚½‹——£  ƒJƒƒ‰‚ÌˆÊ’ui‰ñ“]‚Í–³‹Ij
            transform.position = target.position + offset;
        }
    }
}