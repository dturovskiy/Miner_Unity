using System.Collections.Generic;
using MinerUnity.Runtime;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Replaces TerrainController and TerrainGeneration GameObjects logic.
    /// Dynamically spawns tiles near the camera and destroys them when out of range.
    /// Fog of war is separate and revealed based on hero position.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform hero;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Tilemap hiddenAreaFog; // "Fog of war" tilemap

        [Header("Loaded Area Around Camera")]
        [SerializeField, Min(0)] private int preloadBlocksX = 3;
        [SerializeField, Min(0)] private int preloadBlocksY = 5;
        [SerializeField, Min(0)] private int keepAliveMarginX = 2;
        [SerializeField, Min(0)] private int keepAliveMarginY = 3;

        [Header("Fog Of War Around Hero")]
        [SerializeField, Min(0)] private int fogRevealRadiusX = 3;
        [SerializeField, Min(0)] private int fogRevealRadiusUp = 4;
        [SerializeField, Min(0)] private int fogRevealRadiusDown = 2;

        // The gameplay-facing owner of world queries and mutations.
        private WorldRuntime worldRuntime;
        private WorldData worldData => worldRuntime?.WorldData;
        private byte[] fogGrid; // For storing fog of war state (1 = explored, 0 = hidden)
        private string fogPath;
        private bool fogDirty;
        private float nextFogSaveTime;
        [SerializeField] private float fogSaveInterval = 1.5f;

        [Header("Stone Fall Timing")]
        [SerializeField, Min(0f)] private float stoneWarningDelay = 0.7f;
        [SerializeField] private CameraFollow cameraFollow;

        private struct ScheduledStoneFall
        {
            public Vector2Int Position;
            public float ExecuteAt;
        }

        private readonly Dictionary<Vector2Int, ScheduledStoneFall> scheduledFalls = new();
        private readonly List<Vector2Int> dueFalls = new();

        private StoneGravityService stoneGravityService => worldRuntime?.StoneGravityService;

        // Keeps track of which GameObjects are currently physically spawned in the scene
        private Dictionary<Vector2Int, GameObject> spawnedTiles = new Dictionary<Vector2Int, GameObject>();
        private List<Vector2Int> tilesToDespawn = new List<Vector2Int>();

        private Vector2Int lastHeroPos = new Vector2Int(-999, -999);
        private Vector2Int lastCameraPos = new Vector2Int(-999, -999);
        private bool? lastFogInCaveState;
        private GameSaveData saveData;

        public WorldData GetWorldData() => worldData;
        public WorldRuntime GetWorldRuntime() => worldRuntime;

        private void Start()
        {
            // 1. Ensure world is generated
            var generator = GetComponent<FastTerrainGenerator>();
            if (generator != null)
            {
                generator.GenerateIfMissing();
            }

            worldRuntime = new WorldRuntime(new WorldData());
            bool loadedFromGameSave = TryLoadGameSave();
            bool worldLoaded = loadedFromGameSave;

            string worldSourcePath = GamePersistenceService.SaveFilePath;
            if (!loadedFromGameSave)
            {
                // 2. Fallback to the legacy world file until migration is complete.
                worldSourcePath = System.IO.Path.Combine(Application.persistentDataPath, "world_grid.dat");
                worldLoaded = worldRuntime.LoadFromFile(worldSourcePath);
            }

            if (!worldLoaded)
            {
                Debug.LogError("ChunkManager: Failed to load world grid! Make sure FastTerrainGenerator ran.");
            }

            Diag.Event(
                "World",
                "Loaded",
                worldLoaded ? (loadedFromGameSave ? "World data loaded from game save." : "World data loaded from legacy file.") : "World data failed to load.",
                this,
                ("path", worldSourcePath),
                ("success", worldLoaded),
                ("source", loadedFromGameSave ? "gameSave" : "legacyWorldFile"),
                ("width", worldData.Width),
                ("height", worldData.Height));

            // Load Fog state
            fogGrid = new byte[worldData.Width * worldData.Height];
            fogPath = GamePersistenceService.SaveFilePath;

            bool fogLoaded = false;
            if (loadedFromGameSave && saveData != null)
            {
                fogGrid = GamePersistenceService.CreateFogCopy(saveData, worldData.Width * worldData.Height);
                fogLoaded = true;
            }
            else
            {
                string legacyFogPath = System.IO.Path.Combine(Application.persistentDataPath, "fog_grid.dat");
                if (System.IO.File.Exists(legacyFogPath))
                {
                    byte[] legacyFog = System.IO.File.ReadAllBytes(legacyFogPath);
                    if (legacyFog.Length == fogGrid.Length)
                    {
                        fogGrid = legacyFog;
                        fogLoaded = true;
                    }
                }
            }

            // Clear already explored fog at start setup
            if (hiddenAreaFog != null)
            {
                for (int x = 0; x < worldData.Width; x++)
                {
                    for (int y = 0; y < worldData.Height; y++)
                    {
                        if (fogGrid[y * worldData.Width + x] == 1)
                        {
                            hiddenAreaFog.SetTile(new Vector3Int(x, y, 0), null);
                        }
                    }
                }
            }

            if (hero != null && saveData != null)
            {
                hero.position = GamePersistenceService.GetHeroPositionOrDefault(saveData, hero.position);
            }

            Diag.Event(
                "Fog",
                "Loaded",
                fogLoaded ? (loadedFromGameSave ? "Fog data loaded from game save." : "Fog data loaded from legacy file.") : "Fog data file not found. Starting with hidden fog.",
                this,
                ("path", fogPath),
                ("success", fogLoaded),
                ("source", loadedFromGameSave ? "gameSave" : "legacyFogFile"),
                ("bytes", fogGrid != null ? fogGrid.Length : 0),
                ("exploredCount", CountExploredFogCells()),
                ("tilemapAssigned", hiddenAreaFog != null));

            if (!loadedFromGameSave && worldLoaded)
            {
                SaveGameData();
            }

            // 3. Clear fog of war logic around starting area immediately
            ForceUpdateChunks();
        }

        private void Update()
        {
            if (hero == null) return;
            if (worldCamera == null) worldCamera = Camera.main;

            Vector2Int heroCell = WorldToCell(hero.position);
            Vector2Int cameraCell = worldCamera != null ? WorldToCell(worldCamera.transform.position) : heroCell;

            // 1. Підвантаження рахуємо по камері
            if (cameraCell != lastCameraPos)
            {
                lastCameraPos = cameraCell;
                UpdateLoadedTiles();
            }

            // 2. Відкриття fog рахуємо по герою
            if (heroCell != lastHeroPos)
            {
                lastHeroPos = heroCell;
                RevealFogAroundHero(heroCell);
            }

            ProcessScheduledStoneFalls();

            if (fogDirty && Time.time >= nextFogSaveTime)
            {
                SaveFogData();
            }
        }

        private Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y)
            );
        }

        private void ForceUpdateChunks()
        {
            if (hero == null) return;
            if (worldCamera == null) worldCamera = Camera.main;

            Vector2Int heroCell = WorldToCell(hero.position);
            Vector2Int cameraCell = worldCamera != null ? WorldToCell(worldCamera.transform.position) : heroCell;

            lastHeroPos = heroCell;
            lastCameraPos = cameraCell;

            RevealFogAroundHero(heroCell);
            UpdateLoadedTiles();
        }

        private void GetCameraBoundsInCells(int extraX, int extraY, out int minX, out int maxX, out int minY, out int maxY)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;

            if (cam == null)
            {
                Vector2Int fallbackCenter = WorldToCell(hero.position);

                minX = Mathf.Max(0, fallbackCenter.x - 15 - extraX);
                maxX = Mathf.Min(worldData.Width - 1, fallbackCenter.x + 15 + extraX);
                minY = Mathf.Max(0, fallbackCenter.y - 10 - extraY);
                maxY = Mathf.Min(worldData.Height - 1, fallbackCenter.y + 10 + extraY);
                return;
            }

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Vector3 camPos = cam.transform.position;

            minX = Mathf.Max(0, Mathf.FloorToInt(camPos.x - halfWidth) - extraX);
            maxX = Mathf.Min(worldData.Width - 1, Mathf.CeilToInt(camPos.x + halfWidth) + extraX);

            minY = Mathf.Max(0, Mathf.FloorToInt(camPos.y - halfHeight) - extraY);
            maxY = Mathf.Min(worldData.Height - 1, Mathf.CeilToInt(camPos.y + halfHeight) + extraY);
        }

        private void UpdateLoadedTiles()
        {
            GetCameraBoundsInCells(
                preloadBlocksX,
                preloadBlocksY,
                out int loadMinX,
                out int loadMaxX,
                out int loadMinY,
                out int loadMaxY
            );

            GetCameraBoundsInCells(
                preloadBlocksX + keepAliveMarginX,
                preloadBlocksY + keepAliveMarginY,
                out int keepMinX,
                out int keepMaxX,
                out int keepMinY,
                out int keepMaxY
            );

            int spawnedCount = 0;

            for (int x = loadMinX; x <= loadMaxX; x++)
            {
                for (int y = loadMinY; y <= loadMaxY; y++)
                {
                    TileID id = worldData.GetTile(x, y);

                    if (id == TileID.Empty || id == TileID.Edge)
                    {
                        continue;
                    }

                    Vector2Int pos = new Vector2Int(x, y);

                    if (!spawnedTiles.ContainsKey(pos))
                    {
                        SpawnTileGameObject(pos, id);
                        spawnedCount++;
                    }
                }
            }

            tilesToDespawn.Clear();

            foreach (var kvp in spawnedTiles)
            {
                Vector2Int pos = kvp.Key;

                bool outsideKeepAlive =
                    pos.x < keepMinX || pos.x > keepMaxX ||
                    pos.y < keepMinY || pos.y > keepMaxY;

                if (outsideKeepAlive)
                {
                    tilesToDespawn.Add(pos);
                }
            }

            int despawnedCount = tilesToDespawn.Count;

            for (int i = 0; i < tilesToDespawn.Count; i++)
            {
                Vector2Int pos = tilesToDespawn[i];
                Destroy(spawnedTiles[pos]);
                spawnedTiles.Remove(pos);
            }

            Diag.Event(
                "Chunk",
                "LoadedAreaUpdated",
                "Loaded area updated around camera.",
                this,
                ("cameraCell", lastCameraPos),
                ("loadMin", new Vector2Int(loadMinX, loadMinY)),
                ("loadMax", new Vector2Int(loadMaxX, loadMaxY)),
                ("keepMin", new Vector2Int(keepMinX, keepMinY)),
                ("keepMax", new Vector2Int(keepMaxX, keepMaxY)),
                ("spawned", spawnedCount),
                ("despawned", despawnedCount),
                ("activeTiles", spawnedTiles.Count));
        }

        private void RevealFogAroundHero(Vector2Int center)
        {
            if (hiddenAreaFog == null)
            {
                Diag.Warning("Fog", "Skipped", "Fog reveal skipped: hiddenAreaFog is missing.", this,
                    ("reason", "hiddenAreaFogMissing"),
                    ("center", center));
                return;
            }

            if (hero == null)
            {
                Diag.Warning("Fog", "Skipped", "Fog reveal skipped: hero is missing.", this,
                    ("reason", "heroMissing"),
                    ("center", center));
                return;
            }

            if (!worldData.IsValidCoordinate(center.x, center.y))
            {
                Diag.Warning("Fog", "Skipped", "Fog reveal skipped: hero cell is outside world bounds.", this,
                    ("reason", "centerOutOfBounds"),
                    ("center", center));
                return;
            }

            bool inCave = worldData.GetTile(center.x, center.y) == TileID.Tunnel;
            if (!inCave)
            {
                if (lastFogInCaveState != false)
                {
                    Diag.Event("Fog", "Skipped", "Fog reveal skipped: hero is outside cave.", this,
                        ("reason", "outsideCave"),
                        ("center", center));
                }

                lastFogInCaveState = false;
                return;
            }

            lastFogInCaveState = true;

            bool fogChanged = false;
            int revealedCount = 0;

            for (int x = -fogRevealRadiusX; x <= fogRevealRadiusX; x++)
            {
                for (int y = -fogRevealRadiusDown; y <= fogRevealRadiusUp; y++)
                {
                    int worldX = center.x + x;
                    int worldY = center.y + y;

                    if (worldX < 0 || worldX >= worldData.Width || worldY < 0 || worldY >= worldData.Height)
                    {
                        continue;
                    }

                    Vector3Int fogPos = new Vector3Int(worldX, worldY, 0);

                    if (hiddenAreaFog.GetTile(fogPos) != null)
                    {
                        hiddenAreaFog.SetTile(fogPos, null);
                        fogGrid[worldY * worldData.Width + worldX] = 1;
                        fogChanged = true;
                        revealedCount++;
                    }
                }
            }

            if (fogChanged)
            {
                if (!string.IsNullOrEmpty(fogPath))
                {
                    fogDirty = true;
                }

                Diag.Event(
                    "Fog",
                    "Revealed",
                    "Fog area revealed around hero.",
                    this,
                    ("center", center),
                    ("revealedCount", revealedCount),
                    ("radiusX", fogRevealRadiusX),
                    ("radiusUp", fogRevealRadiusUp),
                    ("radiusDown", fogRevealRadiusDown),
                    ("fogDirty", fogDirty));
            }
        }

        private void SpawnTileGameObject(Vector2Int pos, TileID id)
        {
            TileClass tClass = TileRegistry.Instance.GetTileClass(id);
            if (tClass == null) return;

            GameObject newTile = new GameObject(tClass.tileName);
            newTile.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0);
            newTile.transform.SetParent(this.transform);

            SpriteRenderer sr = newTile.AddComponent<SpriteRenderer>();
            sr.sprite = tClass.tileSprite;

            if (id == TileID.Edge)
            {
                newTile.tag = "Edge";
            }

            if (id != TileID.Tunnel)
            {
                BoxCollider2D coll = newTile.AddComponent<BoxCollider2D>();
                coll.size = Vector2.one;

                if (id == TileID.Ladder)
                {
                    coll.isTrigger = true;
                    newTile.tag = "Ladder";
                }
                else if (id != TileID.Edge)
                {
                    newTile.tag = "Ground";
                }
            }

            if (id == TileID.Stone)
            {
                newTile.tag = "Stone";

                StoneView stoneView = newTile.AddComponent<StoneView>();

                if (scheduledFalls.TryGetValue(pos, out ScheduledStoneFall scheduled))
                {
                    float remaining = Mathf.Max(0f, scheduled.ExecuteAt - Time.time);
                    if (remaining > 0f)
                    {
                        stoneView.PlayWarning(remaining);
                    }
                }
            }

            TileBehaviour tb = newTile.AddComponent<TileBehaviour>();
            tb.gridX = pos.x;
            tb.gridY = pos.y;

            spawnedTiles[pos] = newTile;
        }

        public void DestroyTileInWorld(int x, int y)
        {
            if (worldRuntime == null || !worldRuntime.TryDestroyTile(x, y, out _))
            {
                return;
            }

            Vector2Int removedPos = new Vector2Int(x, y);
            if (spawnedTiles.TryGetValue(removedPos, out GameObject removedGo))
            {
                spawnedTiles.Remove(removedPos);
                Destroy(removedGo);
            }

            TryScheduleStoneAfterSupportLoss(x, y + 1);
            SaveWorldData();
        }

        public void PlaceTileInWorld(int x, int y, TileID id)
        {
            if (worldRuntime == null || !worldRuntime.TryPlaceTile(x, y, id))
            {
                return;
            }

            if (IsCellInsideLoadedArea(new Vector2Int(x, y)))
            {
                SpawnTileGameObject(new Vector2Int(x, y), id);
            }

            SaveWorldData();
        }

        private void TryScheduleStoneAfterSupportLoss(int x, int y)
        {
            Vector2Int pos = new Vector2Int(x, y);

            if (scheduledFalls.ContainsKey(pos))
            {
                return;
            }

            if (worldRuntime == null || !worldRuntime.CanStoneStartFalling(x, y))
            {
                return;
            }

            scheduledFalls[pos] = new ScheduledStoneFall
            {
                Position = pos,
                ExecuteAt = Time.time + stoneWarningDelay
            };

            Diag.Event(
                "Stone",
                "FallScheduled",
                "Stone fall scheduled after support loss.",
                this,
                ("from", pos),
                ("executeAt", Time.time + stoneWarningDelay),
                ("delay", stoneWarningDelay));

            if (spawnedTiles.TryGetValue(pos, out GameObject stoneGo))
            {
                StoneView stoneView = stoneGo.GetComponent<StoneView>();
                if (stoneView != null)
                {
                    stoneView.PlayWarning(stoneWarningDelay);
                }
            }

            if (cameraFollow != null)
            {
                cameraFollow.PlayStoneWarningShake();
            }
        }

        private void ProcessScheduledStoneFalls()
        {
            if (scheduledFalls.Count == 0)
            {
                return;
            }

            dueFalls.Clear();

            foreach (var pair in scheduledFalls)
            {
                if (Time.time >= pair.Value.ExecuteAt)
                {
                    dueFalls.Add(pair.Key);
                }
            }

            for (int i = 0; i < dueFalls.Count; i++)
            {
                Vector2Int from = dueFalls[i];
                scheduledFalls.Remove(from);

                if (worldRuntime == null || !worldRuntime.TryMoveStoneToLanding(from.x, from.y, out StoneFallResult result))
                {
                    Diag.Event(
                        "Stone",
                        "FallCancelled",
                        "Scheduled stone fall was cancelled before execution.",
                        this,
                        ("from", from),
                        ("reason", "tryMoveFailed"));
                    continue;
                }

                MoveSpawnedStoneView(result.From, result.To);

                Diag.Event(
                    "Stone",
                    "FallExecuted",
                    "Stone fall executed.",
                    this,
                    ("from", result.From),
                    ("to", result.To));

                SaveWorldData();
                TryScheduleStoneAfterSupportLoss(result.From.x, result.From.y + 1);
            }
        }

        private void MoveSpawnedStoneView(Vector2Int from, Vector2Int to)
        {
            if (!spawnedTiles.TryGetValue(from, out GameObject stoneGo))
            {
                if (IsCellInsideLoadedArea(to))
                {
                    SpawnTileGameObject(to, TileID.Stone);
                }
                return;
            }

            spawnedTiles.Remove(from);
            spawnedTiles[to] = stoneGo;

            StoneView stoneView = stoneGo.GetComponent<StoneView>();
            if (stoneView != null)
            {
                stoneView.PlayFallToGridY(to.y);
            }
            else
            {
                stoneGo.transform.position = new Vector3(to.x + 0.5f, to.y + 0.5f, 0f);
            }

            var tb = stoneGo.GetComponent<TileBehaviour>();
            if (tb != null)
            {
                tb.gridX = to.x;
                tb.gridY = to.y;
            }
        }

        private void SaveWorldData()
        {            
            SaveGameData();

            Diag.Event(
                "World",
                "Saved",
                "World data saved.",
                this,
                ("path", GamePersistenceService.SaveFilePath),
                ("format", "GameSaveData"),
                ("width", worldData.Width),
                ("height", worldData.Height));
        }

        private void SaveFogData()
        {
            if (string.IsNullOrEmpty(fogPath) || fogGrid == null || !fogDirty)
            {
                return;
            }

            SaveGameData();
            fogDirty = false;
            nextFogSaveTime = Time.time + fogSaveInterval;

            Diag.Event(
                "Fog",
                "Saved",
                "Fog data saved.",
                this,
                ("path", GamePersistenceService.SaveFilePath),
                ("format", "GameSaveData"),
                ("bytes", fogGrid.Length),
                ("exploredCount", CountExploredFogCells()),
                ("nextSaveTime", nextFogSaveTime));
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveFogData();
                SaveWorldData();
            }
        }

        private void OnApplicationQuit()
        {
            SaveFogData();
            SaveWorldData();
        }

        private bool IsCellInsideLoadedArea(Vector2Int pos)
        {
            GetCameraBoundsInCells(
                preloadBlocksX,
                preloadBlocksY,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY
            );

            return pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
        }

        private int CountExploredFogCells()
        {
            if (fogGrid == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < fogGrid.Length; i++)
            {
                if (fogGrid[i] == 1)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryLoadGameSave()
        {
            if (!GamePersistenceService.TryLoad(out saveData))
            {
                saveData = null;
                return false;
            }

            return GamePersistenceService.TryRestoreWorld(saveData, worldRuntime.WorldData);
        }

        private void SaveGameData()
        {
            if (worldRuntime == null || worldData == null)
            {
                return;
            }

            saveData ??= GamePersistenceService.CreateFromRuntime(
                worldData,
                fogGrid,
                hero != null ? hero.position : Vector3.zero);

            GamePersistenceService.ApplyRuntimeState(
                saveData,
                worldData,
                fogGrid,
                hero != null ? hero.position : Vector3.zero);

            GamePersistenceService.Save(saveData);
        }
    }
}
