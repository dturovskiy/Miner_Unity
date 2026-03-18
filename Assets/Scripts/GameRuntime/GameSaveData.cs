using System;
using System.Collections.Generic;

namespace MinerUnity.Runtime
{
    /// <summary>
    /// Root persistence model for the whole game.
    /// World, hero, and progression state should all hang off this object.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 1;
        public WorldSaveData world = new();
        public HeroSaveData hero = new();
        public ProgressionSaveData progression = new();
    }

    [Serializable]
    public sealed class WorldSaveData
    {
        public int width = 100;
        public int height = 255;
        public byte[] tiles = Array.Empty<byte>();
        public byte[] fog = Array.Empty<byte>();
        public List<PlacedObjectData> placedObjects = new();
    }

    [Serializable]
    public sealed class HeroSaveData
    {
        public float positionX;
        public float positionY;
        public float positionZ;
        public string equippedToolId = string.Empty;
        public List<ItemStackData> inventory = new();
    }

    [Serializable]
    public sealed class ProgressionSaveData
    {
        public int coins;
        public List<string> purchasedToolIds = new();
        public List<string> unlockedUpgradeIds = new();
    }

    [Serializable]
    public sealed class PlacedObjectData
    {
        public string objectId = string.Empty;
        public string typeId = string.Empty;
        public int cellX;
        public int cellY;
        public string payloadJson = string.Empty;
    }

    [Serializable]
    public sealed class ItemStackData
    {
        public string itemId = string.Empty;
        public int count;
    }
}
