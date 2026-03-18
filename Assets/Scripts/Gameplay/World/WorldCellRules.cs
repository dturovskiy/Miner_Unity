using UnityEngine;

/// <summary>
/// Єдине місце, де описані правила типів клітинок.
/// </добре: Не розкидай ці правила по різних скриптах.
/// </summary>
public static class WorldCellRules
{
    public static WorldCellFlags GetFlags(WorldCellType type)
    {
        switch (type)
        {
            case WorldCellType.Empty:
                return WorldCellFlags.Passable;

            case WorldCellType.Dirt:
                return WorldCellFlags.Solid | WorldCellFlags.Mineable;

            case WorldCellType.Stone:
                return WorldCellFlags.Solid;

            case WorldCellType.Cave:
                return WorldCellFlags.Passable;

            case WorldCellType.Ladder:
                // Ключова ідея:
                // драбина НЕ solid, але passable і climbable.
                return WorldCellFlags.Passable | WorldCellFlags.Climbable;

            default:
                return WorldCellFlags.None;
        }
    }

    public static bool IsSolid(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Solid) != 0;
    }

    public static bool IsPassable(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Passable) != 0;
    }

    public static bool IsClimbable(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Climbable) != 0;
    }

    public static bool IsMineable(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Mineable) != 0;
    }
}
