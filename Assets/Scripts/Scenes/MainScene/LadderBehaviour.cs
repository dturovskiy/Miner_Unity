using UnityEngine;

/// <summary>
/// Дані однієї драбини.
/// Тут НІЯКОЇ логіки станів героя немає.
/// Цей компонент лише дає геометрію:
/// - центр драбини по X
/// - верхню межу
/// - нижню межу
/// - зручні позиції, куди ставити центр героя
///   на верхньому краї або внизу драбини
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LadderBehaviour : MonoBehaviour
{
    [Header("Hero Alignment")]
    [SerializeField] private float feetSkin = 0.01f;

    private Collider2D cachedCollider;

    /// <summary>
    /// Кешуємо колайдер драбини.
    /// Для драбини краще використовувати Collider2D з IsTrigger = true.
    /// </summary>
    public Collider2D LadderCollider
    {
        get
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider2D>();
            }

            return cachedCollider;
        }
    }

    /// <summary>
    /// Світові межі драбини.
    /// </summary>
    public Bounds WorldBounds => LadderCollider.bounds;

    /// <summary>
    /// Центр драбини по X.
    /// Героя під час climb ми вирівнюємо саме сюди.
    /// </summary>
    public float CenterX => WorldBounds.center.x;

    /// <summary>
    /// Верхня межа драбини.
    /// </summary>
    public float TopY => WorldBounds.max.y;

    /// <summary>
    /// Нижня межа драбини.
    /// </summary>
    public float BottomY => WorldBounds.min.y;

    /// <summary>
    /// Висота драбини в world units.
    /// </summary>
    public float Height => WorldBounds.size.y;

    /// <summary>
    /// Позиція центру героя, коли він стоїть зверху на цій драбині.
    /// Тобто ноги героя рівно на верхній межі блока драбини.
    /// </summary>
    public float GetTopStandCenterY(Collider2D heroCollider)
    {
        return TopY + heroCollider.bounds.extents.y - feetSkin;
    }

    /// <summary>
    /// Позиція центру героя, коли він знаходиться внизу драбини.
    /// Це нижня безпечна позиція, нижче якої ми не даємо спускатися,
    /// якщо під поточною драбиною вже немає продовження.
    /// </summary>
    public float GetBottomStandCenterY(Collider2D heroCollider)
    {
        return BottomY + heroCollider.bounds.extents.y - feetSkin;
    }
}
