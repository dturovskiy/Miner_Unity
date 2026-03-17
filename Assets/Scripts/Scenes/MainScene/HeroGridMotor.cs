using UnityEngine;

/// <summary>
/// Абсолютно новий контролер руху героя.
/// Він працює не через "стан драбини", а через правила клітинок grid.
///
/// Ключові властивості:
/// - герой не стрибає;
/// - драбина є passable + climbable;
/// - герой може рухатись по X всередині драбини;
/// - герой може стояти на верхній межі верхнього блока драбини;
/// - герой не може лізти вище цієї межі.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HeroInputReader))]
public sealed class HeroGridMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGridService worldGrid;
    [SerializeField] private HeroInputReader inputReader;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float climbSpeed = 2.5f;
    [SerializeField] private float fallSpeed = 5.5f;

    [Header("Body")]
    [SerializeField] private Vector2 bodySize = new Vector2(0.72f, 1.80f);
    [SerializeField] private float skin = 0.02f;

    private Rigidbody2D rb;

    public bool IsGrounded { get; private set; }
    public bool IsInsideLadder { get; private set; }
    public bool IsStandingOnLadderTop { get; private set; }
    public bool IsFalling { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (inputReader == null)
        {
            inputReader = GetComponent<HeroInputReader>();
        }

        // Важливо:
        // фізика Unity тут не веде героя.
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (worldGrid == null || inputReader == null)
        {
            return;
        }

        Vector2 currentPosition = rb.position;
        Vector2 nextPosition = currentPosition;

        float dt = Time.fixedDeltaTime;
        float inputX = inputReader.Horizontal;
        float inputY = inputReader.Vertical;

        // Спочатку оновлюємо поточні derived-state прапорці.
        RefreshDerivedState(currentPosition);

        // 1. Вертикальний рух або утримання / падіння.
        nextPosition = ResolveVertical(nextPosition, inputY, dt);

        // 2. Горизонтальний рух.
        nextPosition = ResolveHorizontal(nextPosition, inputX, dt);

        // 3. Після переміщення ще раз стабілізуємо опору.
        nextPosition = ResolveSupportSnap(nextPosition);

        // 4. Оновлюємо прапорці для animator / іншої логіки.
        RefreshDerivedState(nextPosition);

        rb.MovePosition(nextPosition);
    }

    private Vector2 ResolveVertical(Vector2 position, float inputY, float dt)
    {
        // Рух вгору дозволений тільки якщо ноги всередині драбини.
        if (inputY > 0f)
        {
            if (TryGetLadderCellForUp(position, out Vector2Int ladderCell))
            {
                float nextFeetY = GetFeetY(position) + climbSpeed * inputY * dt;

                Vector2Int topLadderCell = FindTopOfLadderColumn(ladderCell);

                // Найвища допустима позиція ніг:
                // верхня межа верхнього блока драбини.
                float maxFeetY = worldGrid.GetCellTopY(topLadderCell.y);

                if (nextFeetY > maxFeetY)
                {
                    nextFeetY = maxFeetY;
                }

                Vector2 next = SetFeetY(position, nextFeetY);

                // Не даємо герою зайти тілом у solid-клітинки.
                if (CanOccupy(next))
                {
                    return next;
                }
            }

            return position;
        }

        // Рух вниз дозволений:
        // - якщо герой вже в драбині;
        // - або якщо стоїть на верхній межі драбини.
        if (inputY < 0f)
        {
            if (TryGetLadderCellForDown(position, out _))
            {
                float nextFeetY = GetFeetY(position) + climbSpeed * inputY * dt;
                Vector2 next = SetFeetY(position, nextFeetY);

                if (CanOccupy(next))
                {
                    return next;
                }
            }

            return position;
        }

        // Якщо інпуту по Y немає:
        // - всередині драбини герой НЕ падає;
        // - на верхній межі драбини герой НЕ падає;
        // - без опори і без драбини — падає.
        if (IsFeetInsideLadder(position) || IsStandingOnTopOfLadder(position))
        {
            return position;
        }

        if (TryGetSupportY(position, out float supportY))
        {
            return SetFeetY(position, supportY);
        }

        float fallingFeetY = GetFeetY(position) - fallSpeed * dt;
        Vector2 fallingPosition = SetFeetY(position, fallingFeetY);

        if (TryGetSupportY(fallingPosition, out float snappedSupportY))
        {
            return SetFeetY(fallingPosition, snappedSupportY);
        }

        if (CanOccupy(fallingPosition))
        {
            return fallingPosition;
        }

        return position;
    }

    private Vector2 ResolveHorizontal(Vector2 position, float inputX, float dt)
    {
        if (Mathf.Abs(inputX) <= 0.001f)
        {
            return position;
        }

        float deltaX = inputX * walkSpeed * dt;
        Vector2 next = position + new Vector2(deltaX, 0f);

        // На драбині X теж вільний.
        if (CanOccupy(next))
        {
            return next;
        }

        return position;
    }

    private Vector2 ResolveSupportSnap(Vector2 position)
    {
        // Якщо герой стоїть на solid-опорі або на верхній межі драбини,
        // підсаджуємо ноги точно на потрібний Y.
        if (TryGetSupportY(position, out float supportY))
        {
            return SetFeetY(position, supportY);
        }

        return position;
    }

    private void RefreshDerivedState(Vector2 position)
    {
        IsInsideLadder = IsFeetInsideLadder(position);
        IsStandingOnLadderTop = IsStandingOnTopOfLadder(position);
        IsGrounded = TryGetSupportY(position, out _);
        IsFalling = !IsGrounded && !IsInsideLadder;
    }

    private bool CanOccupy(Vector2 bodyPosition)
    {
        Rect rect = GetBodyRect(bodyPosition);

        Vector2Int minCell = worldGrid.WorldToCell(new Vector2(rect.xMin + skin, rect.yMin + skin));
        Vector2Int maxCell = worldGrid.WorldToCell(new Vector2(rect.xMax - skin, rect.yMax - skin));

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                // Якщо будь-яка перекрита клітинка solid — тут стояти не можна.
                if (worldGrid.IsSolid(cell))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool TryGetSupportY(Vector2 position, out float supportY)
    {
        // 1. Звичайна тверда опора знизу.
        if (TryGetSolidSupportY(position, out supportY))
        {
            return true;
        }

        // 2. Спеціальна опора: верхня межа верхнього блока драбини.
        if (TryGetLadderTopSupportY(position, out supportY))
        {
            return true;
        }

        supportY = 0f;
        return false;
    }

    private bool TryGetSolidSupportY(Vector2 position, out float supportY)
    {
        Rect rect = GetBodyRect(position);
        float feetY = rect.yMin;

        Vector2 leftFootProbe = new Vector2(rect.xMin + skin, feetY - skin);
        Vector2 rightFootProbe = new Vector2(rect.xMax - skin, feetY - skin);

        Vector2Int leftCell = worldGrid.WorldToCell(leftFootProbe);
        Vector2Int rightCell = worldGrid.WorldToCell(rightFootProbe);

        bool leftSolid = worldGrid.IsSolid(leftCell);
        bool rightSolid = worldGrid.IsSolid(rightCell);

        if (!leftSolid && !rightSolid)
        {
            supportY = 0f;
            return false;
        }

        int supportCellY = leftSolid ? leftCell.y : rightCell.y;

        // Якщо обидві точки на solid, беремо вищу межу.
        if (leftSolid && rightSolid)
        {
            supportCellY = Mathf.Max(leftCell.y, rightCell.y);
        }

        supportY = worldGrid.GetCellTopY(supportCellY);
        return true;
    }

    private bool TryGetLadderTopSupportY(Vector2 position, out float supportY)
    {
        float feetY = GetFeetY(position);
        Vector2Int feetCell = GetFeetCell(position);
        Vector2Int belowFeetCell = feetCell + Vector2Int.down;

        // Має бути так:
        // - поточна клітинка ніг вже НЕ ladder;
        // - під ногами є ladder.
        if (worldGrid.IsClimbable(feetCell) || !worldGrid.IsClimbable(belowFeetCell))
        {
            supportY = 0f;
            return false;
        }

        Vector2Int topLadderCell = FindTopOfLadderColumn(belowFeetCell);
        float ladderTopY = worldGrid.GetCellTopY(topLadderCell.y);

        // Якщо ноги близько до верхньої межі —
        // вважаємо це стійкою позицією на верху драбини.
        float tolerance = worldGrid.CellSize * 0.2f;

        if (Mathf.Abs(feetY - ladderTopY) <= tolerance)
        {
            supportY = ladderTopY;
            return true;
        }

        supportY = 0f;
        return false;
    }

    private bool TryGetLadderCellForUp(Vector2 position, out Vector2Int ladderCell)
    {
        // Вгору можна тільки коли ноги реально в ladder-клітинці.
        Vector2Int feetCell = GetFeetCell(position);

        if (worldGrid.IsClimbable(feetCell))
        {
            ladderCell = feetCell;
            return true;
        }

        ladderCell = default;
        return false;
    }

    private bool TryGetLadderCellForDown(Vector2 position, out Vector2Int ladderCell)
    {
        Vector2Int feetCell = GetFeetCell(position);
        Vector2Int belowFeetCell = feetCell + Vector2Int.down;

        // Вниз можна:
        // - якщо ноги вже в ladder;
        // - або якщо герой стоїть на верхній межі драбини.
        if (worldGrid.IsClimbable(feetCell))
        {
            ladderCell = feetCell;
            return true;
        }

        if (worldGrid.IsClimbable(belowFeetCell))
        {
            ladderCell = belowFeetCell;
            return true;
        }

        ladderCell = default;
        return false;
    }

    private Vector2Int findTopOfLadderColumn(Vector2Int startCell)
    {
        Vector2Int current = startCell;

        while (worldGrid.IsClimbable(current + Vector2Int.up))
        {
            current += Vector2Int.up;
        }

        return current;
    }

    // Додано для сумісності з капіталізованою версією у викликах
    private Vector2Int FindTopOfLadderColumn(Vector2Int startCell) => findTopOfLadderColumn(startCell);

    private bool IsFeetInsideLadder(Vector2 position)
    {
        Vector2Int feetCell = GetFeetCell(position);
        return worldGrid.IsClimbable(feetCell);
    }

    private bool IsStandingOnTopOfLadder(Vector2 position)
    {
        return TryGetLadderTopSupportY(position, out _);
    }

    private Vector2Int GetFeetCell(Vector2 bodyPosition)
    {
        // Ноги беремо трохи нижче фактичної межі,
        // щоб на boundary не стрибати між двома клітинками.
        Vector2 feetProbe = new Vector2(bodyPosition.x, GetFeetY(bodyPosition) - skin);
        return worldGrid.WorldToCell(feetProbe);
    }

    private float GetFeetY(Vector2 bodyPosition)
    {
        return bodyPosition.y - bodySize.y * 0.5f;
    }

    private Vector2 SetFeetY(Vector2 bodyPosition, float feetY)
    {
        return new Vector2(bodyPosition.x, feetY + bodySize.y * 0.5f);
    }

    private Rect GetBodyRect(Vector2 bodyPosition)
    {
        return new Rect(
            bodyPosition.x - bodySize.x * 0.5f,
            bodyPosition.y - bodySize.y * 0.5f,
            bodySize.x,
            bodySize.y
        );
    }
}
