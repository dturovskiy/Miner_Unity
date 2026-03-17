using UnityEngine;

/// <summary>
/// Новий контролер копання.
/// Він працює не через overlap по колайдерах,
/// а через сусідню клітинку grid.
/// Це робить копання з драбини абсолютно природним.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class MiningController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGridService worldGrid;
    [SerializeField] private Joystick miningJoystick;

    [Header("Timing")]
    [SerializeField] private float miningDelay = 0.4f;
    [SerializeField] private float inputThreshold = 0.5f;

    private Animator animator;
    private float miningStartTime;
    private bool isMining;
    private Vector2Int currentTargetCell;

    public bool IsMining => isMining;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
    }

    private void Start()
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
    }

    private void Update()
    {
        if (worldGrid == null || miningJoystick == null)
        {
            StopMining();
            return;
        }

        Vector2Int direction = ReadMiningDirection();

        if (direction == Vector2Int.zero)
        {
            StopMining();
            return;
        }

        Vector2Int bodyCell = worldGrid.WorldToCell(transform.position);
        Vector2Int targetCell = bodyCell + direction;

        if (!worldGrid.IsMineable(targetCell))
        {
            StopMining();
            return;
        }

        if (!isMining || currentTargetCell != targetCell)
        {
            StartMining(targetCell);
        }

        if (Time.time - miningStartTime >= miningDelay)
        {
            BreakCell(targetCell);
            StopMining();
        }
    }

    private Vector2Int ReadMiningDirection()
    {
        float x = miningJoystick.Horizontal;
        float y = miningJoystick.Vertical;

        bool hasX = Mathf.Abs(x) >= inputThreshold;
        bool hasY = Mathf.Abs(y) >= inputThreshold;

        if (!hasX && !hasY)
        {
            return Vector2Int.zero;
        }

        // Беремо домінуючу вісь.
        if (Mathf.Abs(y) > Mathf.Abs(x))
        {
            return y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        return x > 0f ? Vector2Int.right : Vector2Int.left;
    }

    private void StartMining(Vector2Int targetCell)
    {
        isMining = true;
        currentTargetCell = targetCell;
        miningStartTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsMining", true);
        }
    }

    private void StopMining()
    {
        isMining = false;

        if (animator != null)
        {
            animator.SetBool("IsMining", false);
        }
    }

    private void BreakCell(Vector2Int cell)
    {
        // Мінімальний варіант:
        // просто перетворюємо mineable-клітинку на Empty.
        // Якщо захочеш HP/міцність по типах — додається поверх цього шару.
        worldGrid.SetCellType(cell, WorldCellType.Empty);
        
        // Тут також варто викликати ChunkManager.DestroyTileInWorld, якщо він існує,
        // щоб оновити візуал і worldData.
        var chunkManager = Object.FindFirstObjectByType<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(cell.x, cell.y);
        }
    }
}
