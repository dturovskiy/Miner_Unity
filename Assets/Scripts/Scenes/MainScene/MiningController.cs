using UnityEngine;

public sealed class MiningController : MonoBehaviour
{
    [SerializeField] private WorldGridService worldGrid;

    // Для діагностики руху краще тимчасово не читати той самий joystick,
    // який використовується для ходьби.
    [SerializeField] private Joystick miningJoystick;

    [SerializeField] private Animator animator;
    [SerializeField] private float miningDelay = 0.4f;

    private float timer;
    private Vector2Int currentTarget;

    public bool IsMining { get; private set; }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (worldGrid == null)
        {
            worldGrid = WorldGridService.Instance;
        }

        // Поки лагодимо рух, можна просто вийти,
        // якщо окремий joystick для копання не призначений.
        if (worldGrid == null || miningJoystick == null)
        {
            StopMining();
            return;
        }

        Vector2 input = new Vector2(miningJoystick.Horizontal, miningJoystick.Vertical);

        if (input.magnitude < 0.5f)
        {
            StopMining();
            return;
        }

        Vector2Int dir = Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            dir.x = input.x > 0 ? 1 : -1;
        }
        else
        {
            dir.y = input.y > 0 ? 1 : -1;
        }

        Vector2Int target = worldGrid.WorldToCell(transform.position) + dir;

        if (!worldGrid.IsMineable(target))
        {
            StopMining();
            return;
        }

        if (!IsMining || currentTarget != target)
        {
            IsMining = true;
            currentTarget = target;
            timer = 0f;
        }

        timer += Time.deltaTime;

        if (timer >= miningDelay)
        {
            BreakCell(target);
            timer = 0f;
        }

        if (animator != null)
        {
            animator.SetBool("IsMining", IsMining);
        }
    }

    private void BreakCell(Vector2Int cell)
    {
        worldGrid.SetCellType(cell, WorldCellType.Empty);

        // Для Unity 2020.3 безпечніше використовувати FindObjectOfType.
        var chunkManager = Object.FindObjectOfType<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(cell.x, cell.y);
        }
    }

    private void StopMining()
    {
        IsMining = false;
        timer = 0f;

        if (animator != null)
        {
            animator.SetBool("IsMining", false);
        }
    }
}

