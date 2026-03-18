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
        // Встановлюємо героя ТАК, щоб ноги були на 0.01 нижче за край драбини.
        // Це гарантує миттєве спрацювання GroundCheck і запобігає підстрибуванню.
        float heroHalfHeight = heroCollider.bounds.extents.y;
        return TopY + heroHalfHeight - 0.01f;
    }

    public float GetBottomStandY(Collider2D heroCollider)
    {
        float heroHalfHeight = heroCollider.bounds.extents.y;
        return BottomY + heroHalfHeight;
    }

    private void Reset()
    {
        ladderCollider = GetComponent<Collider2D>();
    }
}
