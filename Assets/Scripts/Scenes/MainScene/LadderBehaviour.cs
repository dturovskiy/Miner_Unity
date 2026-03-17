using UnityEngine;

/// <summary>
/// Дані конкретної драбини.
/// Саме звідси мотор драбини бере:
/// - центр по X
/// - верхню точку виходу
/// - нижню точку входу/виходу
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class LadderBehaviour : MonoBehaviour
{
    [SerializeField] private Collider2D ladderCollider;

    [Header("Offsets")]
    [SerializeField] private float topExitOffset = 0.05f;
    [SerializeField] private float bottomExitOffset = 0.05f;

    public Bounds Bounds
    {
        get
        {
            if (ladderCollider == null)
            {
                ladderCollider = GetComponent<Collider2D>();
            }

            return ladderCollider.bounds;
        }
    }

    public float CenterX => Bounds.center.x;
    public float TopY => Bounds.max.y;
    public float BottomY => Bounds.min.y;

    public float GetTopStandY(Collider2D heroCollider)
    {
        float heroHalfHeight = heroCollider.bounds.extents.y;
        return TopY + heroHalfHeight + topExitOffset;
    }

    public float GetBottomStandY(Collider2D heroCollider)
    {
        float heroHalfHeight = heroCollider.bounds.extents.y;
        return BottomY + heroHalfHeight + bottomExitOffset;
    }

    private void Reset()
    {
        ladderCollider = GetComponent<Collider2D>();
    }
}
