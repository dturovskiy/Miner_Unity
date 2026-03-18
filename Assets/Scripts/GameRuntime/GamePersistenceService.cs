using System;
using System.IO;
using MinerUnity.Terrain;
using UnityEngine;

namespace MinerUnity.Runtime
{
    /// <summary>
    /// Central persistence service for the new root save model.
    /// Legacy world and fog files are only used as migration inputs.
    /// </summary>
    public static class GamePersistenceService
    {
        private const string SaveFileName = "game_save.json";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }

        public static bool TryLoad(out GameSaveData saveData)
        {
            saveData = null;

            if (!HasSave())
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                saveData = JsonUtility.FromJson<GameSaveData>(json);
                EnsureInitialized(saveData);
                return saveData != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GamePersistenceService: Failed to load save file. {exception.Message}");
                saveData = null;
                return false;
            }
        }

        public static void Save(GameSaveData saveData)
        {
            EnsureInitialized(saveData);

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, json);
        }

        public static GameSaveData CreateFromRuntime(WorldData worldData, byte[] fogGrid, Vector3 heroPosition)
        {
            var saveData = new GameSaveData();
            ApplyRuntimeState(saveData, worldData, fogGrid, heroPosition);
            return saveData;
        }

        public static void ApplyRuntimeState(GameSaveData saveData, WorldData worldData, byte[] fogGrid, Vector3 heroPosition)
        {
            EnsureInitialized(saveData);

            saveData.world.width = worldData.Width;
            saveData.world.height = worldData.Height;
            saveData.world.tiles = worldData.ToByteArray();
            saveData.world.fog = CloneOrEmpty(fogGrid);

            saveData.hero.positionX = heroPosition.x;
            saveData.hero.positionY = heroPosition.y;
            saveData.hero.positionZ = heroPosition.z;
            saveData.hero.hasPosition = true;
        }

        public static bool TryRestoreWorld(GameSaveData saveData, WorldData worldData)
        {
            EnsureInitialized(saveData);
            return saveData != null && worldData.LoadFromBytes(saveData.world.tiles);
        }

        public static byte[] CreateFogCopy(GameSaveData saveData, int expectedLength)
        {
            EnsureInitialized(saveData);

            if (saveData == null || saveData.world.fog == null || saveData.world.fog.Length != expectedLength)
            {
                return new byte[expectedLength];
            }

            return CloneOrEmpty(saveData.world.fog);
        }

        public static Vector3 GetHeroPositionOrDefault(GameSaveData saveData, Vector3 fallback)
        {
            EnsureInitialized(saveData);

            if (saveData == null)
            {
                return fallback;
            }

            if (!saveData.hero.hasPosition)
            {
                return fallback;
            }

            return new Vector3(
                saveData.hero.positionX,
                saveData.hero.positionY,
                saveData.hero.positionZ);
        }

        private static byte[] CloneOrEmpty(byte[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var copy = new byte[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static void EnsureInitialized(GameSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.world ??= new WorldSaveData();
            saveData.hero ??= new HeroSaveData();
            saveData.progression ??= new ProgressionSaveData();

            saveData.world.tiles ??= Array.Empty<byte>();
            saveData.world.fog ??= Array.Empty<byte>();
            saveData.world.placedObjects ??= new System.Collections.Generic.List<PlacedObjectData>();

            saveData.hero.equippedToolId ??= string.Empty;
            saveData.hero.inventory ??= new System.Collections.Generic.List<ItemStackData>();

            saveData.progression.purchasedToolIds ??= new System.Collections.Generic.List<string>();
            saveData.progression.unlockedUpgradeIds ??= new System.Collections.Generic.List<string>();
        }
    }
}
