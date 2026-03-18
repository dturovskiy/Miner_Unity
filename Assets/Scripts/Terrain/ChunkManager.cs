using System.Collections.Generic;
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

        // The single source of truth for the entire map (25KB)
        private WorldData worldData;
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

        private StoneGravityService stoneGravityService;

        // Keeps track of which GameObjects are currently physically spawned in the scene
        private Dictionary<Vector2Int, GameObject> spawnedTiles = new Dictionary<Vector2Int, GameObject>();
        private List<Vector2Int> tilesToDespawn = new List<Vector2Int>();

        private Vector2Int lastHeroPos = new Vector2Int(-999, -999);
        private Vector2Int lastCameraPos = new Vector2Int(-999, -999);

        public WorldData GetWorldData() => worldData;

        private void Start()
        {
            // 1. Ensure world is generated
            var generator = GetComponent<FastTerrainGenerator>();
            if (generator != null)
            {
                generator.GenerateIfMissing();
            }

            // 2. Load the world data from disk into memory
            worldData = new WorldData();
            string path = System.IO.Path.Combine(Application.persistentDataPath, "world_grid.dat");
            if (!worldData.LoadFromFile(path))
            {
                Debug.LogError("ChunkManager: Failed to load world grid! Make sure FastTerrainGenerator ran.");
            }

            stoneGravityService = new StoneGravityService(worldData);

            // Load Fog state
            fogGrid = new byte[worldData.Width * worldData.Height];
            fogPath = System.IO.Path.Combine(Application.persistentDataPath, "fog_grid.dat");
            if (System.IO.File.Exists(fogPath))
            {
                fogGrid = System.IO.File.ReadAllBytes(fogPath);
                
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

            // Для orthographic-камери halfHeight задається orthographicSize
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Vector3 camPos = cam.transform.position;

            // Додаємо preload/extras, щоб спавнити трохи поза межами екрана
            minX = Mathf.Max(0, Mathf.FloorToInt(camPos.x - halfWidth) - extraX);
            maxX = Mathf.Min(worldData.Width - 1, Mathf.CeilToInt(camPos.x + halfWidth) + extraX);

            minY = Mathf.Max(0, Mathf.FloorToInt(camPos.y - halfHeight) - extraY);
            maxY = Mathf.Min(worldData.Height - 1, Mathf.CeilToInt(camPos.y + halfHeight) + extraY);
        }

        private void UpdateLoadedTiles()
        {
            // 1. Межі, де тайли точно мають бути завантажені
            GetCameraBoundsInCells(
                preloadBlocksX,
                preloadBlocksY,
                out int loadMinX,
                out int loadMaxX,
                out int loadMinY,
                out int loadMaxY
            );

            // 2. Межі, де тайли ще дозволено тримати в пам'яті,
            // навіть якщо вони вже трохи вийшли за екран.
            GetCameraBoundsInCells(
                preloadBlocksX + keepAliveMarginX,
                preloadBlocksY + keepAliveMarginY,
                out int keepMinX,
                out int keepMaxX,
                out int keepMinY,
                out int keepMaxY
            );

            // 3. Спавнимо все, що потрібно в зоні камери
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
                    }
                }
            }

            // 4. Деспавнимо лише ті тайли, що вже далеко за межами keep-alive області
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

            for (int i = 0; i < tilesToDespawn.Count; i++)
            {
                Vector2Int pos = tilesToDespawn[i];
                Destroy(spawnedTiles[pos]);
                spawnedTiles.Remove(pos);
            }
        }

        private void RevealFogAroundHero(Vector2Int center)
        {
            if (hiddenAreaFog == null || hero == null)
            {
                return;
            }

            if (!worldData.IsValidCoordinate(center.x, center.y))
            {
                return;
            }

            bool inCave = worldData.GetTile(center.x, center.y) == TileID.Tunnel;
            if (!inCave)
            {
                return;
            }

            bool fogChanged = false;

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
                    }
                }
            }

            if (fogChanged && !string.IsNullOrEmpty(fogPath))
            {
                fogDirty = true;
            }
        }

        private void SpawnTileGameObject(Vector2Int pos, TileID id)
        {
            TileClass tClass = TileRegistry.Instance.GetTileClass(id);
            if (tClass == null) return;

            GameObject newTile = new GameObject(tClass.tileName);
            newTile.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0);
            newTile.transform.SetParent(this.transform);

            // Sprite
            SpriteRenderer sr = newTile.AddComponent<SpriteRenderer>();
            sr.sprite = tClass.tileSprite;

            // Physics & Gameplay logic (from old TerrainGeneration)
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

                // Якщо камінь уже стоїть у черзі на падіння,
                // відтворюємо решту warning-time після спавну.
                if (scheduledFalls.TryGetValue(pos, out ScheduledStoneFall scheduled))
                {
                    float remaining = Mathf.Max(0f, scheduled.ExecuteAt - Time.time);
                    if (remaining > 0f)
                    {
                        stoneView.PlayWarning(remaining);
                    }
                }
            }

            // Add the behaviour that handles mining
            TileBehaviour tb = newTile.AddComponent<TileBehaviour>();
            // Store coordinates so TileBehaviour knows what to tell WorldData to delete
            tb.gridX = pos.x;
            tb.gridY = pos.y;

            // Notice we DO NOT use TransformSaver anymore - it's handled by world_grid.dat array!

            spawnedTiles[pos] = newTile;
        }

        /// <summary>
        /// Called by TileBehaviour when it's destroyed by the player 4 hits.
        /// </summary>
        public void DestroyTileInWorld(int x, int y)
        {
            // 1. Видаляємо тайл із даних світу.
            worldData.SetTile(x, y, TileID.Empty);

            // 2. Якщо в'юшка цієї клітинки існує — прибираємо її.
            Vector2Int removedPos = new Vector2Int(x, y);
            if (spawnedTiles.TryGetValue(removedPos, out GameObject removedGo))
            {
                spawnedTiles.Remove(removedPos);
                Destroy(removedGo);
            }

            // 3. Після втрати опори перевіряємо лише камінь безпосередньо над зруйнованим блоком.
            TryScheduleStoneAfterSupportLoss(x, y + 1);

            // 4. Зберігаємо новий стан карти.
            SaveWorldData();
        }

        /// <summary>
        /// Called when a tile is placed at runtime (e.g. Ladder).
        /// </summary>
        public void PlaceTileInWorld(int x, int y, TileID id)
        {
            worldData.SetTile(x, y, id);
            
            // Якщо клітинка в білій зоні - спавнимо візуал негайно
            if (IsCellInsideLoadedArea(new Vector2Int(x, y)))
            {
                SpawnTileGameObject(new Vector2Int(x, y), id);
            }
            
            SaveWorldData();
        }

        private void TryScheduleStoneAfterSupportLoss(int x, int y)
        {
            Vector2Int pos = new Vector2Int(x, y);

            // Уже стоїть у черзі - не дублюємо.
            if (scheduledFalls.ContainsKey(pos))
            {
                return;
            }

            // Якщо це не камінь або в нього ще є опора - нічого не робимо.
            if (!stoneGravityService.CanStoneStartFalling(x, y))
            {
                return;
            }

            scheduledFalls[pos] = new ScheduledStoneFall
            {
                Position = pos,
                ExecuteAt = Time.time + stoneWarningDelay
            };

            // Якщо камінь зараз у сцені - запускаємо його локальне попередження.
            if (spawnedTiles.TryGetValue(pos, out GameObject stoneGo))
            {
                StoneView stoneView = stoneGo.GetComponent<StoneView>();
                if (stoneView != null)
                {
                    stoneView.PlayWarning(stoneWarningDelay);
                }
            }

            // Глобальна тряска камери.
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

            // Спочатку збираємо всі готові падіння в окремий список,
            // щоб безпечно змінювати dictionary після цього.
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

                // Перевіряємо стан ще раз у момент старту падіння.
                // За час затримки щось могло змінитися.
                if (!stoneGravityService.TryMoveStoneToLanding(from.x, from.y, out StoneFallResult result))
                {
                    continue;
                }

                // Пересуваємо в'юшку, якщо вона заспавнена.
                MoveSpawnedStoneView(result.From, result.To);

                // Зберігаємо новий стан після фактичного переміщення.
                SaveWorldData();

                // Після того як нижній камінь зрушив,
                // камінь над старою клітинкою міг втратити опору.
                TryScheduleStoneAfterSupportLoss(result.From.x, result.From.y + 1);
            }
        }

        private void MoveSpawnedStoneView(Vector2Int from, Vector2Int to)
        {
            // Якщо старий камінь не був заспавнений,
            // значить він був поза екраном.
            // Дані світу вже правильні, тому нічого страшного.
            // При наступному оновленні чанків він з’явиться одразу на новому місці.
            if (!spawnedTiles.TryGetValue(from, out GameObject stoneGo))
            {
                // Камінь був поза спавном. Якщо тепер його нова клітинка
                // потрапляє в активну область камери — заспавнимо його одразу.
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
                // Якщо раптом компонента немає —
                // просто жорстко переносимо об'єкт.
                stoneGo.transform.position = new Vector3(to.x + 0.5f, to.y + 0.5f, 0f);
            }

            // Оновлюємо координати для TileBehaviour
            var tb = stoneGo.GetComponent<TileBehaviour>();
            if (tb != null)
            {
                tb.gridX = to.x;
                tb.gridY = to.y;
            }
        }

        private void SaveWorldData()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "world_grid.dat");
            worldData.SaveToFile(path);
        }

        private void SaveFogData()
        {
            if (string.IsNullOrEmpty(fogPath) || fogGrid == null || !fogDirty)
            {
                return;
            }

            System.IO.File.WriteAllBytes(fogPath, fogGrid);
            fogDirty = false;
            nextFogSaveTime = Time.time + fogSaveInterval;
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
    }
}
