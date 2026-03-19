using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class HeroGroundSensor : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D capsule;
    [SerializeField] private LayerMask solidMask = ~0;
    [SerializeField, Min(0.005f)] private float groundProbeDistance = 0.08f;
    [SerializeField, Range(0.1f, 1f)] private float probeWidthFactor = 0.9f;

    private readonly Collider2D[] overlapHits = new Collider2D[8];

    public CapsuleCollider2D Capsule => capsule;
    public LayerMask SolidMask => solidMask;
    public float GroundProbeDistance => groundProbeDistance;
    public float ProbeWidthFactor => probeWidthFactor;

    private void Reset()
    {
        capsule = GetComponent<CapsuleCollider2D>();
    }

    private void Awake()
    {
        capsule ??= GetComponent<CapsuleCollider2D>();
    }

    public bool IsGrounded()
    {
        return TryGetGroundHit(out _);
    }

    public bool TryGetGroundHit(out Collider2D hit)
    {
        if (capsule == null)
        {
            hit = null;
            return false;
        }

        GetGroundProbe(out Vector2 center, out Vector2 size);
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = solidMask,
            useTriggers = false
        };

        int hitCount = Physics2D.OverlapBox(center, size, 0f, filter, overlapHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D candidate = overlapHits[i];
            if (candidate != null && candidate != capsule)
            {
                hit = candidate;
                return true;
            }
        }

        hit = null;
        return false;
    }

    public void GetGroundProbe(out Vector2 center, out Vector2 size)
    {
        if (capsule == null)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return;
        }

        Bounds bounds = capsule.bounds;
        size = new Vector2(bounds.size.x * probeWidthFactor, groundProbeDistance);
        center = new Vector2(bounds.center.x, bounds.min.y - groundProbeDistance * 0.5f);
    }
}
