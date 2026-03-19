using System.Collections.Generic;
using UnityEngine;

public sealed class HeroDebugContextProvider : MonoBehaviour, IDiagContextProvider
{
    [SerializeField] private HeroController heroController;
    [SerializeField] private HeroState heroState;
    [SerializeField] private HeroCollision heroCollision;

    private void Reset()
    {
        heroController = GetComponent<HeroController>();
        heroState = GetComponent<HeroState>();
        heroCollision = GetComponent<HeroCollision>();
    }

    public void AppendContext(List<DiagField> fields)
    {
        fields.Add(new DiagField("object", gameObject.name));
        fields.Add(new DiagField("position", DiagFieldBag.Stringify(transform.position)));

        if (heroState != null)
        {
            fields.Add(new DiagField("locomotion", heroState.Locomotion.ToString()));
        }

        if (heroController != null)
        {
            fields.Add(new DiagField("inputX", DiagFieldBag.Stringify(heroController.HorizontalInput)));
            fields.Add(new DiagField("velocityX", DiagFieldBag.Stringify(heroController.CurrentSpeedX)));
            fields.Add(new DiagField("velocityY", DiagFieldBag.Stringify(heroController.CurrentSpeedY)));
        }

        if (heroCollision != null)
        {
            bool worldReady = heroCollision.IsWorldReady();
            fields.Add(new DiagField("worldReady", DiagFieldBag.Stringify(worldReady)));
            fields.Add(new DiagField("bootstrapPending", DiagFieldBag.Stringify(!worldReady)));

            if (worldReady)
            {
                fields.Add(new DiagField("cell", heroCollision.GetCurrentCell().ToString()));
                fields.Add(new DiagField("footCell", heroCollision.GetFootCell().ToString()));
                fields.Add(new DiagField("footTile", heroCollision.GetFootTileId().ToString()));
                fields.Add(new DiagField("footType", heroCollision.GetFootCellType().ToString()));
            }
        }
    }
}
