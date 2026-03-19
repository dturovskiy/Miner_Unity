using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    public int gridX;
    public int gridY;

    [SerializeField] private Crack crackView;
    [SerializeField, Range(0, 3)] private int crackStage;

    public void SetCrackStage(int stage)
    {
        int normalizedStage = Mathf.Clamp(stage, 0, 3);
        if (normalizedStage == crackStage)
        {
            return;
        }

        crackStage = normalizedStage;
        if (crackStage <= 0)
        {
            ClearCrack();
            return;
        }

        EnsureCrackView();
        crackView?.SetStage(crackStage);
        string crackSprite = crackView != null ? crackView.CurrentSpriteName : "None";

        Diag.Event(
            "Tile",
            "CrackStageChanged",
            "Tile crack stage updated.",
            this,
            ("cellX", gridX),
            ("cellY", gridY),
            ("crackStage", crackStage),
            ("crackSprite", crackSprite));
    }

    public void ClearCrack()
    {
        bool hadCrack = crackStage > 0 || crackView != null;
        crackStage = 0;
        if (crackView != null)
        {
            crackView.gameObject.SetActive(false);
            Destroy(crackView.gameObject);
            crackView = null;
        }

        if (hadCrack)
        {
            Diag.Event(
                "Tile",
                "CrackCleared",
                "Tile crack visuals cleared.",
                this,
                ("cellX", gridX),
                ("cellY", gridY));
        }
    }

    public void BreakTile()
    {
        var chunkManager = GetComponentInParent<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(gridX, gridY);
        }
    }

    private void EnsureCrackView()
    {
        if (crackView != null)
        {
            return;
        }

        GameObject crackObject = new GameObject("Crack");
        crackObject.layer = gameObject.layer;
        crackObject.transform.SetParent(transform);
        crackObject.transform.localPosition = Vector3.zero;
        crackObject.transform.localRotation = Quaternion.identity;
        crackObject.transform.localScale = Vector3.one;

        SpriteRenderer tileRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer crackRenderer = crackObject.AddComponent<SpriteRenderer>();
        crackRenderer.color = Color.white;

        if (tileRenderer != null)
        {
            crackRenderer.sortingLayerID = tileRenderer.sortingLayerID;
            crackRenderer.sortingOrder = tileRenderer.sortingOrder + 1;
            crackRenderer.sharedMaterial = tileRenderer.sharedMaterial;
            crackRenderer.maskInteraction = tileRenderer.maskInteraction;
            crackRenderer.spriteSortPoint = tileRenderer.spriteSortPoint;
        }

        crackView = crackObject.AddComponent<Crack>();
    }
}
