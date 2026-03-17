using UnityEngine;

/// <summary>
/// Опис однієї драбини.
/// Цей скрипт не рухає героя і не керує станами.
/// Він лише дає геометрію драбини в world space.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LadderBehaviour : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField] private float feetSkin = 0.01f;

    private Collider2D cachedCollider;

    /// <summary>
    /// Кешований колайдер драбини.
    /// Для драбини краще мати Collider2D з IsTrigger = true.
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
    /// Межі драбини у світових координатах.
    /// </summary>
    public Bounds Bounds => LadderCollider.bounds;

    /// <summary>
    /// Центр драбини по X.
    /// Під час climb героя завжди притягуємо до цього X.
    /// </summary>
    public float CenterX => Bounds.center.x;

    /// <summary>
    /// Верхня межа колайдера драбини.
    /// </summary>
    public float TopY => Bounds.max.y;

    /// <summary>
    /// Нижня межа колайдера драбини.
    /// </summary>
    public float BottomY => Bounds.min.y;

    /// <summary>
    /// Позиція центру героя, коли він стоїть зверху на цій драбині.
    /// Тобто ноги героя знаходяться рівно на верхній площині драбини.
    /// </summary>
    public float GetTopStandCenterY(Collider2D heroCollider)
    {
        return TopY + heroCollider.bounds.extents.y - feetSkin;
    }

    /// <summary>
    /// Позиція центру героя, коли він спустився до низу цієї драбини.
    /// </summary>
    public float GetBottomStandCenterY(Collider2D heroCollider)
    {
        return BottomY + heroCollider.bounds.extents.y - feetSkin;
    }
}
