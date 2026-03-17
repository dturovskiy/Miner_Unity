using UnityEngine;

/// <summary>
/// Грід-базований контролер копання.
/// Працює незалежно від стану руху.
/// </summary>
public sealed class MiningController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGridService worldGrid;
    [SerializeField] private Joystick miningJoystick;
    public float miningDelay = 0.4f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;

    private float timer;
    private Vector2Int currentTarget;
    public bool IsMining { get; private set; }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
        if (miningJoystick == null || worldGrid == null) return;

        Vector2 input = new Vector2(miningJoystick.Horizontal, miningJoystick.Vertical);
        
        if (input.magnitude < 0.5f)
        {
            Stop();
            return;
        }

        // Визначаємо напрямок копання (4 сторони)
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

        if (worldGrid.IsMineable(target))
        {
            if (!IsMining || currentTarget != target)
            {
                IsMining = true;
                currentTarget = target;
                timer = 0;
            }

            timer += Time.deltaTime;
            
            if (timer >= miningDelay)
            {
                BreakCell(target);
                timer = 0;
            }
        }
        else
        {
            Stop();
        }

        if (animator != null)
        {
            animator.SetBool("IsMining", IsMining);
        }
    }

    private void BreakCell(Vector2Int cell)
    {
        worldGrid.SetCellType(cell, WorldCellType.Empty);
        
        // Синхронізація з ChunkManager для візуального видалення
        var chunkManager = Object.FindFirstObjectByType<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(cell.x, cell.y);
        }
    }

    private void Stop()
    {
        IsMining = false;
        timer = 0;
    }
}
