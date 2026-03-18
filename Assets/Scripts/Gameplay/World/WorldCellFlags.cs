using System;

/// <summary>
/// Властивості клітинки.
/// Можна комбінувати через прапорці.
/// </summary>
[Flags]
public enum WorldCellFlags
{
    None = 0,
    Solid = 1 << 0,
    Passable = 1 << 1,
    Climbable = 1 << 2,
    Mineable = 1 << 3
}
