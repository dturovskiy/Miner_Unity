using UnityEngine;

public sealed class DiagManualProbe : MonoBehaviour
{
    [SerializeField] private string category = "Probe";
    [SerializeField] private string eventName = "Manual";

    [ContextMenu("Emit Probe Event")]
    public void EmitProbeEvent()
    {
        Diag.Event(category, eventName, "Manual probe from inspector.", this,
            ("position", transform.position),
            ("active", gameObject.activeInHierarchy));
    }
}
