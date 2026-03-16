namespace MinerUnity.Terrain
{
    /// <summary>
    /// ID of every possible tile in the game.
    /// Using byte (0-255) ensures the entire 100x255 map fits into ~25KB.
    /// 0 is always Empty/Air.
    /// </summary>
    public enum TileID : byte
    {
        Empty = 0,
        
        // Environment
        Stone = 1,
        Dirt = 2,
        Tunnel = 3,
        Edge = 4,   // Invisible boundary blocks

        // Ores
        Coal = 10,
        Iron = 11,
        Gold = 12,
        Diamond = 13,
        Uranus = 14,
        Topaz = 15,
        Silver = 16,
        Ruby = 17,
        Platinum = 18,
        Opal = 19,
        Nephritis = 20,
        Map = 21,
        Lazurite = 22,
        Emerald = 23,
        Artifact = 24,
        Amethyst = 25
    }
}
