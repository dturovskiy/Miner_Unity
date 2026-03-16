using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Replaces TerrainController and TerrainGeneration GameObjects logic.
    /// Dynamically spawns tiles near the hero and destroys them when out of range.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform hero;
        [SerializeField] private Tilemap hiddenAreaFog; // "Fog of war" tilemap
        
        [Header("Settings")]
        [SerializeField] private int viewDistanceX = 15;
        [SerializeField] private int viewDistanceY = 10;
        
        // The single source of truth for the entire map (25KB)
        private WorldData worldData;
        private byte[] fogGrid; // For storing fog of war state (1 = explored, 0 = hidden)
        private string fogPath;

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
            if (hero != null)
            {
                Vector2Int currentHeroPos = new Vector2Int(Mathf.FloorToInt(hero.position.x), Mathf.FloorToInt(hero.position.y));

                // Only update chunks if hero moved to a new block
                if (currentHeroPos != lastHeroPos)
                {
                    lastHeroPos = currentHeroPos;
                    UpdateViewArea(currentHeroPos);
                }
            }

            ProcessScheduledStoneFalls();
        }

        private void ForceUpdateChunks()
        {
            if (hero == null) return;
            Vector2Int currentHeroPos = new Vector2Int(Mathf.FloorToInt(hero.position.x), Mathf.FloorToInt(hero.position.y));
            lastHeroPos = currentHeroPos;
            UpdateViewArea(currentHeroPos);
        }

        private void UpdateViewArea(Vector2Int center)
        {
            // 1. Calculate the bounding box of what should be visible
            int minX = Mathf.Max(0, center.x - viewDistanceX);
            int maxX = Mathf.Min(worldData.Width - 1, center.x + viewDistanceX);
            int minY = Mathf.Max(0, center.y - viewDistanceY);
            int maxY = Mathf.Min(worldData.Height - 1, center.y + viewDistanceY);

            // Keep track of tiles we verified are in view
            HashSet<Vector2Int> currentlyVisible = new HashSet<Vector2Int>();

            // 2. Spawn required tiles
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    TileID id = worldData.GetTile(x, y);
                    if (id != TileID.Empty && id != TileID.Edge)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        currentlyVisible.Add(pos);

                        if (!spawnedTiles.ContainsKey(pos))
                        {
                            SpawnTileGameObject(pos, id);
                        }
                    }
                }
            }

            // 2.5 Clear fog of war in a fixed smaller radius (3x, 1y) just like old TerrainController
            if (hiddenAreaFog != null)
            {
                bool inCave = false;
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(hero.position, 0.4f))
                {
                    if (collider.CompareTag("Cave")) inCave = true;
                }

                if (!inCave)
                {
                    int fogRadiusX = 3;
                    int fogRadiusY = 1;
                    int fogCenterY = Mathf.FloorToInt(hero.position.y); // Adjust y independently if needed
                    bool fogChanged = false;

                    for (int x = -fogRadiusX; x <= fogRadiusX; x++)
                    {
                        for (int y = -fogRadiusY; y <= fogRadiusY; y++)
                        {
                            Vector3Int fogPos = new Vector3Int(center.x + x, center.y + y, 0); // old script used heroCellPosition which is similar
                            if (hiddenAreaFog.GetTile(fogPos) != null)
                            {
                                hiddenAreaFog.SetTile(fogPos, null);

                                // Save this securely in byte array mapping
                                if (fogPos.x >= 0 && fogPos.x < worldData.Width && fogPos.y >= 0 && fogPos.y < worldData.Height)
                                {
                                    fogGrid[fogPos.y * worldData.Width + fogPos.x] = 1;
                                    fogChanged = true;
                                }
                            }
                        }
                    }

                    if (fogChanged && fogPath != null)
                    {
                        System.IO.File.WriteAllBytes(fogPath, fogGrid);
                    }
                }
            }

            // 3. Despawn tiles that are now out of view
            tilesToDespawn.Clear();
            foreach (var kvp in spawnedTiles)
            {
                if (!currentlyVisible.Contains(kvp.Key))
                {
                    tilesToDespawn.Add(kvp.Key);
                }
            }

            foreach (var pos in tilesToDespawn)
            {
                Destroy(spawnedTiles[pos]);
                spawnedTiles.Remove(pos);
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
                if (id != TileID.Edge)
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
    }
}
