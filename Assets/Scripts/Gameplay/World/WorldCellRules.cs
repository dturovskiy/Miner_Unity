using MinerUnity.Terrain;
using UnityEngine;

/// <summary>
/// Єдине місце, де описані правила типів клітинок.
/// </добре: Не розкидай ці правила по різних скриптах.
/// </summary>
public static class WorldCellRules
{
    public static WorldCellType GetCellType(TileID id)
    {
        switch (id)
        {
            case TileID.Empty:
            case TileID.Tunnel:
                return WorldCellType.Empty;

            case TileID.Dirt:
            case TileID.Coal:
            case TileID.Iron:
            case TileID.Gold:
            case TileID.Diamond:
            case TileID.Uranus:
            case TileID.Topaz:
            case TileID.Silver:
            case TileID.Ruby:
            case TileID.Platinum:
            case TileID.Opal:
            case TileID.Nephritis:
            case TileID.Map:
            case TileID.Lazurite:
            case TileID.Emerald:
            case TileID.Artifact:
            case TileID.Amethyst:
                return WorldCellType.Dirt;

            case TileID.Stone:
            case TileID.Edge:
                return WorldCellType.Stone;

            case TileID.Ladder:
                return WorldCellType.Ladder;

            default:
                return WorldCellType.Empty;
        }
    }

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

    public static WorldCellFlags GetFlags(TileID id)
    {
        return GetFlags(GetCellType(id));
    }

    public static bool IsSolid(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Solid) != 0;
    }

    public static bool IsSolid(TileID id)
    {
        return (GetFlags(id) & WorldCellFlags.Solid) != 0;
    }

    public static bool IsPassable(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Passable) != 0;
    }

    public static bool IsPassable(TileID id)
    {
        return (GetFlags(id) & WorldCellFlags.Passable) != 0;
    }

    public static bool IsClimbable(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Climbable) != 0;
    }

    public static bool IsClimbable(TileID id)
    {
        return (GetFlags(id) & WorldCellFlags.Climbable) != 0;
    }

    public static bool IsMineable(WorldCellType type)
    {
        return (GetFlags(type) & WorldCellFlags.Mineable) != 0;
    }

    public static bool IsMineable(TileID id)
    {
        return (GetFlags(id) & WorldCellFlags.Mineable) != 0;
    }

    public static bool TryGetDefaultTileId(WorldCellType type, out TileID tileId)
    {
        switch (type)
        {
            case WorldCellType.Empty:
                tileId = TileID.Empty;
                return true;

            case WorldCellType.Dirt:
                tileId = TileID.Dirt;
                return true;

            case WorldCellType.Stone:
                tileId = TileID.Stone;
                return true;

            case WorldCellType.Ladder:
                tileId = TileID.Ladder;
                return true;

            default:
                tileId = TileID.Empty;
                return false;
        }
    }
}
