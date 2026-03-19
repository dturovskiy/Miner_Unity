using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Crack : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static Sprite cachedStage1;
    private static Sprite cachedStage2;
    private static Sprite cachedStage3;

    public int CurrentStage { get; private set; }
    public string CurrentSpriteName => spriteRenderer != null && spriteRenderer.sprite != null
        ? spriteRenderer.sprite.name
        : "None";

    private void Awake()
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        EnsureSpritesLoaded();
    }

    public void SetStage(int stage)
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        EnsureSpritesLoaded();
        CurrentStage = Mathf.Clamp(stage, 0, 3);
        Sprite selectedSprite = null;
        switch (CurrentStage)
        {
            case 0:
                break;
            case 1:
                selectedSprite = cachedStage1;
                break;
            case 2:
                selectedSprite = cachedStage2;
                break;
            case 3:
                selectedSprite = cachedStage3;
                break;
        }

        spriteRenderer.enabled = false;
        spriteRenderer.sprite = selectedSprite;
        spriteRenderer.enabled = selectedSprite != null;
    }

    public void HitCrack(int hit)
    {
        switch (hit)
        {
            case 3:
                SetStage(1);
                break;
            case 2:
                SetStage(2);
                break;
            case 1:
                SetStage(3);
                break;
            default:
                SetStage(0);
                break;
        }
    }

    private void EnsureSpritesLoaded()
    {
        cachedStage1 ??= LoadCrackSprite("Cracks/Crack_0", "Crack_0");
        cachedStage2 ??= LoadCrackSprite("Cracks/Crack_1", "Crack_1");
        cachedStage3 ??= LoadCrackSprite("Cracks/Crack_2", "Crack_2");
    }

    private static Sprite LoadCrackSprite(string resourcePath, string spriteName)
    {
#if UNITY_EDITOR
        Object[] atlasAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Scenes/MainScene/Tiles.png");
        for (int i = 0; i < atlasAssets.Length; i++)
        {
            if (atlasAssets[i] is Sprite atlasSprite && atlasSprite.name == spriteName)
            {
                return atlasSprite;
            }
        }
#endif

        Tile tile = Resources.Load<Tile>(resourcePath);
        if (tile != null && tile.sprite != null)
        {
            return tile.sprite;
        }

        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < loadedSprites.Length; i++)
        {
            Sprite sprite = loadedSprites[i];
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }
}
