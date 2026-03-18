using UnityEngine;

public sealed class DiagGameObjectLifecycle : MonoBehaviour
{
    [SerializeField] private string category = "Lifecycle";
    [SerializeField] private bool logEnableDisable = true;
    [SerializeField] private bool logDestroy = true;

    private void Start()
    {
        Diag.Event(category, "Start", null, this, ("activeInHierarchy", gameObject.activeInHierarchy));
    }

    private void OnEnable()
    {
        if (logEnableDisable)
        {
            Diag.Event(category, "Enabled", null, this, ("activeInHierarchy", gameObject.activeInHierarchy));
        }
    }

    private void OnDisable()
    {
        if (logEnableDisable && Application.isPlaying)
        {
            Diag.Event(category, "Disabled", null, this, ("activeInHierarchy", gameObject.activeInHierarchy));
        }
    }

    private void OnDestroy()
    {
        if (logDestroy && Application.isPlaying)
        {
            Diag.Event(category, "Destroyed", null, this);
        }
    }
}
