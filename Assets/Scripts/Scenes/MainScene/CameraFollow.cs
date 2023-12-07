using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Посилання на об'єкт героя
    public float smoothing = 5f; // Згладжування для плавного руху камери

    private Vector3 offset; // Відстань між камерою і героєм

    private void Awake()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetCamPos = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing);
        }
    }
}
