using System.Collections.Generic;
using UnityEngine;

public sealed class HeroDebugSnapshotProvider : MonoBehaviour, IDiagSnapshotProvider
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private HeroController heroController;
    [SerializeField] private HeroState heroState;
    [SerializeField] private HeroCollision heroCollision;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        heroController = GetComponent<HeroController>();
        heroState = GetComponent<HeroState>();
        heroCollision = GetComponent<HeroCollision>();
    }

    public void AppendSnapshot(List<DiagField> fields)
    {
        fields.Add(new DiagField("position", DiagFieldBag.Stringify(transform.position)));

        if (rb != null)
        {
            fields.Add(new DiagField("velocity", DiagFieldBag.Stringify(rb.linearVelocity)));
            fields.Add(new DiagField("gravityScale", DiagFieldBag.Stringify(rb.gravityScale)));
        }

        if (heroController != null)
        {
            fields.Add(new DiagField("inputX", DiagFieldBag.Stringify(heroController.HorizontalInput)));
        }

        if (heroState != null)
        {
            fields.Add(new DiagField("locomotion", heroState.Locomotion.ToString()));
        }

        if (heroCollision != null)
        {
            bool worldReady = heroCollision.IsWorldReady();
            fields.Add(new DiagField("worldReady", DiagFieldBag.Stringify(worldReady)));
            fields.Add(new DiagField("bootstrapPending", DiagFieldBag.Stringify(!worldReady)));

            if (worldReady)
            {
                fields.Add(new DiagField("grounded", DiagFieldBag.Stringify(heroCollision.IsGrounded())));
                fields.Add(new DiagField("cell", heroCollision.GetCurrentCell().ToString()));
                fields.Add(new DiagField("footCell", heroCollision.GetFootCell().ToString()));
                fields.Add(new DiagField("footTile", heroCollision.GetFootTileId().ToString()));
                fields.Add(new DiagField("footType", heroCollision.GetFootCellType().ToString()));

                if (heroCollision.TryGetCurrentSupportInfo(
                    out Collider2D supportCollider,
                    out Vector2Int supportCell,
                    out MinerUnity.Terrain.TileID supportTileId,
                    out WorldCellType supportCellType))
                {
                    fields.Add(new DiagField("supportObject", supportCollider != null ? supportCollider.name : "None"));
                    fields.Add(new DiagField("supportLayer", supportCollider != null ? LayerMask.LayerToName(supportCollider.gameObject.layer) : "None"));
                    fields.Add(new DiagField("supportCell", supportCell.ToString()));
                    fields.Add(new DiagField("supportTile", supportTileId.ToString()));
                    fields.Add(new DiagField("supportType", supportCellType.ToString()));
                }
            }
        }
    }
}
