using System;
using System.IO;
using MinerUnity.Terrain;
using UnityEngine;

namespace MinerUnity.Runtime
{
    /// <summary>
    /// Central persistence service for the primary save model.
    /// `game_save.json` is the authoritative save for continue flow.
    /// `world_grid.dat` remains the active bootstrap input for new games.
    /// `fog_grid.dat` and `SaveGame.json` remain migration or cleanup inputs only.
    /// </summary>
    public static class GamePersistenceService
    {
        private const string SaveFileName = "game_save.json";
        private const string BootstrapWorldFileName = "world_grid.dat";
        private const string MigrationFogFileName = "fog_grid.dat";
        private const string ObsoleteSceneSaveFileName = "SaveGame.json";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BootstrapWorldFilePath => Path.Combine(Application.persistentDataPath, BootstrapWorldFileName);
        public static string MigrationFogFilePath => Path.Combine(Application.persistentDataPath, MigrationFogFileName);
        public static string ObsoleteSceneSaveFilePath => Path.Combine(Application.persistentDataPath, ObsoleteSceneSaveFileName);

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            bool existed = File.Exists(SaveFilePath);
            if (!existed)
            {
                Diag.Event(
                    "Save",
                    "Skipped",
                    "Save deletion skipped because the save file does not exist.",
                    null,
                    ("reason", "missingFile"),
                    ("path", SaveFilePath));
                return;
            }

            File.Delete(SaveFilePath);
            Diag.Event(
                "Save",
                "Deleted",
                "Primary game save file was deleted.",
                null,
                ("path", SaveFilePath));
        }

        public static void ResetForNewGame()
        {
            bool deletedGameSave = DeleteIfExists(SaveFilePath);
            bool deletedBootstrapWorld = DeleteIfExists(BootstrapWorldFilePath);
            bool deletedMigrationFog = DeleteIfExists(MigrationFogFilePath);
            bool deletedObsoleteSceneSave = DeleteIfExists(ObsoleteSceneSaveFilePath);

            Diag.Event(
                "Save",
                "ResetForNewGame",
                "Persistence files were cleared for a new game start.",
                null,
                ("gameSaveDeleted", deletedGameSave),
                ("bootstrapWorldDeleted", deletedBootstrapWorld),
                ("migrationFogDeleted", deletedMigrationFog),
                ("obsoleteSceneSaveDeleted", deletedObsoleteSceneSave),
                ("savePath", SaveFilePath));
        }

        public static bool TryLoad(out GameSaveData saveData)
        {
            saveData = null;
            bool hasSave = HasSave();

            Diag.Event(
                "Load",
                "Requested",
                "Load requested from the primary game save.",
                null,
                ("path", SaveFilePath),
                ("hasSave", hasSave));

            if (!hasSave)
            {
                Diag.Event(
                    "Load",
                    "Skipped",
                    "Load skipped because no primary save file exists.",
                    null,
                    ("reason", "missingFile"),
                    ("path", SaveFilePath));
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                saveData = JsonUtility.FromJson<GameSaveData>(json);
                EnsureInitialized(saveData);
                bool success = saveData != null;

                if (success)
                {
                    Diag.Event(
                        "Load",
                        "Succeeded",
                        "Primary game save loaded successfully.",
                        null,
                        ("path", SaveFilePath),
                        ("jsonLength", json.Length),
                        ("worldBytes", saveData.world?.tiles != null ? saveData.world.tiles.Length : 0),
                        ("fogBytes", saveData.world?.fog != null ? saveData.world.fog.Length : 0),
                        ("hasHeroPosition", saveData.hero != null && saveData.hero.hasPosition),
                        ("miningDamageCount", saveData.world?.miningDamage != null ? saveData.world.miningDamage.Count : 0),
                        ("placedObjectsCount", saveData.world?.placedObjects != null ? saveData.world.placedObjects.Count : 0));
                }
                else
                {
                    Diag.Warning(
                        "Load",
                        "Failed",
                        "Primary game save deserialized to null.",
                        null,
                        ("reason", "deserializedNull"),
                        ("path", SaveFilePath),
                        ("jsonLength", json.Length));
                }

                return success;
            }
            catch (Exception exception)
            {
                Diag.Warning(
                    "Load",
                    "Failed",
                    "Primary game save failed to load.",
                    null,
                    ("reason", "exception"),
                    ("path", SaveFilePath),
                    ("exceptionType", exception.GetType().Name),
                    ("exceptionMessage", exception.Message));
                Debug.LogWarning($"GamePersistenceService: Failed to load save file. {exception.Message}");
                saveData = null;
                return false;
            }
        }

        public static void Save(GameSaveData saveData)
        {
            EnsureInitialized(saveData);

            Diag.Event(
                "Save",
                "Requested",
                "Save requested for the primary game save.",
                null,
                ("path", SaveFilePath),
                ("hasHeroPosition", saveData?.hero != null && saveData.hero.hasPosition),
                ("worldBytes", saveData?.world?.tiles != null ? saveData.world.tiles.Length : 0),
                ("fogBytes", saveData?.world?.fog != null ? saveData.world.fog.Length : 0),
                ("miningDamageCount", saveData?.world?.miningDamage != null ? saveData.world.miningDamage.Count : 0),
                ("placedObjectsCount", saveData?.world?.placedObjects != null ? saveData.world.placedObjects.Count : 0));

            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SaveFilePath, json);
                Diag.Event(
                    "Save",
                    "Succeeded",
                    "Primary game save was written successfully.",
                    null,
                    ("path", SaveFilePath),
                    ("jsonLength", json.Length),
                    ("hasHeroPosition", saveData?.hero != null && saveData.hero.hasPosition));
            }
            catch (Exception exception)
            {
                Diag.Error(
                    "Save",
                    "Failed",
                    "Primary game save failed to write.",
                    null,
                    ("path", SaveFilePath),
                    ("exceptionType", exception.GetType().Name),
                    ("exceptionMessage", exception.Message));
                throw;
            }
        }

        public static GameSaveData CreateFromRuntime(WorldRuntime worldRuntime, byte[] fogGrid, Vector3 heroPosition)
        {
            var saveData = new GameSaveData();
            ApplyRuntimeState(saveData, worldRuntime, fogGrid, heroPosition);
            return saveData;
        }

        public static void ApplyRuntimeState(GameSaveData saveData, WorldRuntime worldRuntime, byte[] fogGrid, Vector3 heroPosition)
        {
            EnsureInitialized(saveData);
            if (worldRuntime == null)
            {
                return;
            }

            WorldData worldData = worldRuntime.WorldData;

            saveData.world.width = worldData.Width;
            saveData.world.height = worldData.Height;
            saveData.world.tiles = worldData.ToByteArray();
            saveData.world.fog = CloneOrEmpty(fogGrid);
            saveData.world.miningDamage = worldRuntime.CreateMiningDamageSnapshot();

            saveData.hero.positionX = heroPosition.x;
            saveData.hero.positionY = heroPosition.y;
            saveData.hero.positionZ = heroPosition.z;
            saveData.hero.hasPosition = true;
        }

        public static bool TryRestoreWorld(GameSaveData saveData, WorldData worldData)
        {
            EnsureInitialized(saveData);
            bool restored = saveData != null && worldData.LoadFromBytes(saveData.world.tiles);
            if (!restored)
            {
                Diag.Warning(
                    "Load",
                    "WorldRestoreFailed",
                    "World bytes could not be restored from the primary save model.",
                    null,
                    ("hasSaveData", saveData != null),
                    ("worldBytes", saveData?.world?.tiles != null ? saveData.world.tiles.Length : 0));
            }

            return restored;
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

        private static bool DeleteIfExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            return true;
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
            saveData.world.miningDamage ??= new System.Collections.Generic.List<MiningDamageData>();
            saveData.world.placedObjects ??= new System.Collections.Generic.List<PlacedObjectData>();

            saveData.hero.equippedToolId ??= string.Empty;
            saveData.hero.inventory ??= new System.Collections.Generic.List<ItemStackData>();

            saveData.progression.purchasedToolIds ??= new System.Collections.Generic.List<string>();
            saveData.progression.unlockedUpgradeIds ??= new System.Collections.Generic.List<string>();
        }
    }
}
