using System.Collections.Generic;
using UnityEngine;

public sealed class DiagTransformSnapshot : MonoBehaviour, IDiagSnapshotProvider
{
    [SerializeField] private bool includeLocalScale = true;

    public void AppendSnapshot(List<DiagField> fields)
    {
        fields.Add(new DiagField("position", DiagFieldBag.Stringify(transform.position)));
        fields.Add(new DiagField("rotationZ", transform.eulerAngles.z.ToString("0.###")));
        if (includeLocalScale)
        {
            fields.Add(new DiagField("scale", DiagFieldBag.Stringify(transform.localScale)));
        }

        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            fields.Add(new DiagField("rb2dPosition", DiagFieldBag.Stringify(rb2d.position)));
            fields.Add(new DiagField("rb2dVelocity", DiagFieldBag.Stringify(rb2d.linearVelocity)));
            fields.Add(new DiagField("rb2dGravityScale", rb2d.gravityScale.ToString("0.###")));
            fields.Add(new DiagField("rb2dBodyType", rb2d.bodyType.ToString()));
        }

        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            fields.Add(new DiagField("colliderBounds", DiagFieldBag.Stringify(collider2D.bounds)));
            fields.Add(new DiagField("colliderTrigger", collider2D.isTrigger.ToString()));
        }
    }
}
