using UnityEngine;
using System.Collections.Generic;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Maps TileID bytes to the actual TileClass (contains the Sprite) and vice versa.
    /// </summary>
    public class TileRegistry : MonoBehaviour
    {
        public static TileRegistry Instance { get; private set; }

        [Header("Dependency")]
        [SerializeField] private TileAtlas atlas;

        private readonly Dictionary<TileID, TileClass> idToClass = new Dictionary<TileID, TileClass>();
        private readonly Dictionary<string, TileID> nameToId = new Dictionary<string, TileID>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeRegistry();
        }

        private void InitializeRegistry()
        {
            if (atlas == null)
            {
                Debug.LogError("TileAtlas is not assigned in TileRegistry!");
                return;
            }

            // Environment
            Register(TileID.Stone, atlas.stone);
            Register(TileID.Dirt, atlas.dirt);
            Register(TileID.Tunnel, atlas.tunnel);
            
            // Ores
            Register(TileID.Coal, atlas.coal);
            Register(TileID.Iron, atlas.iron);
            Register(TileID.Gold, atlas.gold);
            Register(TileID.Diamond, atlas.diamond);
            Register(TileID.Uranus, atlas.uranus);
            Register(TileID.Topaz, atlas.topaz);
            Register(TileID.Silver, atlas.silver);
            Register(TileID.Ruby, atlas.ruby);
            Register(TileID.Platinum, atlas.platinum);
            Register(TileID.Opal, atlas.opal);
            Register(TileID.Nephritis, atlas.nephritis);
            Register(TileID.Map, atlas.map);
            Register(TileID.Lazurite, atlas.lazurite);
            Register(TileID.Emerald, atlas.emerald);
            Register(TileID.Artifact, atlas.artifact);
            Register(TileID.Amethyst, atlas.amethyst);
        }

        private void Register(TileID id, TileClass tileClass)
        {
            if (tileClass == null) return;
            
            idToClass[id] = tileClass;
            
            // Allow looking up ID by tileClass.name (for saving/loading old logic if needed temporarily)
            if (!string.IsNullOrEmpty(tileClass.name))
            {
                nameToId[tileClass.name] = id;
            }
        }

        public TileClass GetTileClass(TileID id)
        {
            if (id == TileID.Empty || id == TileID.Edge) return null;
            if (idToClass.TryGetValue(id, out TileClass result))
            {
                return result;
            }
            return null;
        }

        public TileID GetIDByName(string tileName)
        {
            if (string.IsNullOrEmpty(tileName)) return TileID.Empty;
            if (nameToId.TryGetValue(tileName, out TileID id))
            {
                return id;
            }
            return TileID.Empty; // Default fallback
        }
    }
}
