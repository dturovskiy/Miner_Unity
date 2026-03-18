using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothing = 5f;

    [Header("Shake")]
    [SerializeField] private float defaultShakeDuration = 0.25f;
    [SerializeField] private float defaultShakeAmplitude = 0.12f;
    [SerializeField] private float shakeFrequency = 28f;
    [SerializeField] private float shakeCooldown = 0.08f;

    private Vector3 offset;

    private float shakeStartTime = -999f;
    private float shakeDuration;
    private float shakeAmplitude;
    private float lastShakeRequestTime = -999f;
    private bool hasOffset;

    private void Awake()
    {
        TryInitializeOffset();
    }

    private void Start()
    {
        SnapToTarget();
    }

    /// <summary>
    /// Короткий публічний метод під warning каменю.
    /// </summary>
    public void PlayStoneWarningShake()
    {
        RequestShake(defaultShakeDuration, defaultShakeAmplitude);
    }

    /// <summary>
    /// Універсальний запит на тряску.
    /// duration - скільки триває shake.
    /// amplitude - максимальний зсув камери.
    /// </summary>
    public void RequestShake(float duration, float amplitude)
    {
        // Захист від спаму, якщо кілька каменів активуються майже одночасно.
        if (Time.time - lastShakeRequestTime < shakeCooldown)
        {
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
            return;
        }

        lastShakeRequestTime = Time.time;
        shakeStartTime = Time.time;
        shakeDuration = duration;
        shakeAmplitude = amplitude;
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        if (!hasOffset)
        {
            TryInitializeOffset();
        }

        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!hasOffset)
        {
            TryInitializeOffset();
        }

        Vector3 targetCamPos = target.position + offset;

        // Важливо: множимо на deltaTime,
        // інакше плавність буде залежати від FPS.
        Vector3 followPosition = Vector3.Lerp(
            transform.position,
            targetCamPos,
            smoothing * Time.deltaTime
        );

        transform.position = followPosition + EvaluateShakeOffset();
    }

    private Vector3 EvaluateShakeOffset()
    {
        if (Time.time >= shakeStartTime + shakeDuration)
        {
            return Vector3.zero;
        }

        float normalized = 1f - ((Time.time - shakeStartTime) / Mathf.Max(0.0001f, shakeDuration));
        float currentAmplitude = shakeAmplitude * normalized;

        float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f * currentAmplitude;
        float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f * currentAmplitude;

        return new Vector3(x, y, 0f);
    }

    private bool TryInitializeOffset()
    {
        if (target == null)
        {
            return false;
        }

        offset = transform.position - target.position;
        hasOffset = true;
        return true;
    }
}
