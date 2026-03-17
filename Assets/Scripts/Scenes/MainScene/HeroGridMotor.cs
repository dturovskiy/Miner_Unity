using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class HeroGridMotor : MonoBehaviour
{
    [Header("Speed")]
    public float walkSpeed = 4f;
    public float climbSpeed = 3f;
    public float fallSpeed = 6f;

    [Header("Collision Settings")]
    public Vector2 bodySize = new Vector2(0.6f, 1.6f);
    public float skin = 0.02f;

    private HeroInputReader input;
    private WorldGridService grid;
    private Rigidbody2D rb;

    public bool IsGrounded { get; private set; }
    public bool IsInsideLadder { get; private set; }
    public bool IsFalling { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        input = GetComponent<HeroInputReader>();
    }

    private void Start()
    {
        grid = WorldGridService.Instance;
    }

    private void FixedUpdate()
    {
        if (grid == null) grid = WorldGridService.Instance;
        if (grid == null || input == null) return;

        float h = input.Horizontal;
        float v = input.Vertical;
        float dt = Time.fixedDeltaTime;

        Vector2 pos = rb.position;
        
        // Оновлюємо стани (драбина, земля)
        UpdateStates(pos);

        Vector2 nextPos = pos;

        // 1. Горизонтальний рух
        if (Mathf.Abs(h) > 0.01f)
        {
            float desiredX = pos.x + h * walkSpeed * dt;
            if (CanOccupy(new Vector2(desiredX, pos.y)))
            {
                nextPos.x = desiredX;
            }
            else
            {
                // Спробуємо піднятися на сходинку
                if (CanOccupy(new Vector2(desiredX, pos.y + 0.3f)))
                {
                    nextPos.x = desiredX;
                    nextPos.y += 0.3f;
                }
            }
        }

        // 2. Вертикальний рух (драбина / падіння)
        if (IsInsideLadder)
        {
            float desiredY = nextPos.y + v * climbSpeed * dt;
            if (CanOccupy(new Vector2(nextPos.x, desiredY)))
            {
                nextPos.y = desiredY;
            }
        }
        else if (IsStandingOnLadderTop(pos) && v < -0.1f)
        {
            // Дозволяємо почати спуск з верху драбини
            nextPos.y += v * climbSpeed * dt;
        }
        else if (!IsGrounded)
        {
            // Падіння
            float desiredY = nextPos.y - fallSpeed * dt;
            if (CanOccupy(new Vector2(nextPos.x, desiredY)))
            {
                nextPos.y = desiredY;
            }
            else
            {
                // Снап до землі
                if (TryGetSupportY(nextPos, out float groundY))
                {
                    nextPos.y = groundY + bodySize.y * 0.5f;
                }
            }
        }

        rb.MovePosition(nextPos);
    }

    private void UpdateStates(Vector2 pos)
    {
        Vector2Int centerCell = grid.WorldToCell(pos);
        IsInsideLadder = grid.IsClimbable(centerCell);
        IsGrounded = TryGetSupportY(pos, out _);
        IsFalling = !IsGrounded && !IsInsideLadder;
    }

    private bool CanOccupy(Vector2 pos)
    {
        if (grid == null) return true;

        // Створюємо Rect тіла героя
        Rect r = new Rect(
            pos.x - bodySize.x * 0.5f + skin,
            pos.y - bodySize.y * 0.5f + skin,
            bodySize.x - skin * 2,
            bodySize.y - skin * 2
        );

        Vector2Int min = grid.WorldToCell(new Vector2(r.xMin, r.yMin));
        Vector2Int max = grid.WorldToCell(new Vector2(r.xMax, r.yMax));

        // Якщо сітка ще не заповнена — дозволяємо рух
        if (!grid.IsReady) return true;

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (grid.IsSolid(new Vector2Int(x, y))) return false;
            }
        }
        return true;
    }

    private bool TryGetSupportY(Vector2 pos, out float groundY)
    {
        Vector2 feet = new Vector2(pos.x, pos.y - bodySize.y * 0.5f);
        // Перевіряємо три точки під ногами (ліва, центр, права)
        float[] xOffsets = { -bodySize.x * 0.4f, 0, bodySize.x * 0.4f };
        
        foreach (float offset in xOffsets)
        {
            Vector2Int cellBelow = grid.WorldToCell(feet + new Vector2(offset, -0.05f));
            if (grid.IsSolid(cellBelow))
            {
                groundY = grid.GetCellTopY(cellBelow.y);
                return true;
            }
        }

        // Перевірка верху драбини
        if (IsStandingOnLadderTop(pos))
        {
            Vector2Int below = grid.WorldToCell(feet + new Vector2(0, -0.05f));
            groundY = grid.GetCellTopY(below.y);
            return true;
        }

        groundY = 0;
        return false;
    }

    private bool IsStandingOnLadderTop(Vector2 pos)
    {
        Vector2 feet = new Vector2(pos.x, pos.y - bodySize.y * 0.5f);
        Vector2Int feetCell = grid.WorldToCell(feet);
        Vector2Int below = feetCell + Vector2Int.down;
        return !grid.IsClimbable(feetCell) && grid.IsClimbable(below);
    }
}
