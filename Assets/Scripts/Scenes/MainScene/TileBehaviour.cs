using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private bool isBroken;
    private bool interacted;
    private float interactionStartTime;
    private float interactionEndTime;

    public bool IsBroken { get { return isBroken; } }
    public bool Interacted { get {  return interacted; } }
    public float InteractionStartTime { get { return interactionStartTime; } }
    public float InteractionEndTime { get { return interactionEndTime; } }

    public void Interact()
    {
        interacted = true;
        interactionStartTime = Time.time;
    }

    public void EndInteraction()
    {
        interactionEndTime = Time.time;
    }
    public void BreakTile()
    {
        Destroy(gameObject);
        isBroken = true;

        Debug.Log("Tile is broken!");
    }


}
