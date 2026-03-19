using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class HeroWallSensor : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D capsule;
    [SerializeField] private LayerMask solidMask = ~0;
    [SerializeField, Min(0.005f)] private float wallProbeDistance = 0.05f;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    public CapsuleCollider2D Capsule => capsule;
    public LayerMask SolidMask => solidMask;
    public float WallProbeDistance => wallProbeDistance;

    private void Reset()
    {
        capsule = GetComponent<CapsuleCollider2D>();
    }

    private void Awake()
    {
        capsule ??= GetComponent<CapsuleCollider2D>();
    }

    public bool IsBlockedHorizontally(float direction)
    {
        return TryGetHorizontalBlockHit(direction, out _);
    }

    public bool TryGetHorizontalBlockHit(float direction, out RaycastHit2D hit)
    {
        if (capsule == null || Mathf.Approximately(direction, 0f))
        {
            hit = default;
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = solidMask,
            useTriggers = false
        };

        int hitCount = capsule.Cast(new Vector2(Mathf.Sign(direction), 0f), filter, castHits, wallProbeDistance);
        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].collider != null && castHits[i].collider != capsule)
            {
                hit = castHits[i];
                return true;
            }
        }

        hit = default;
        return false;
    }
}
