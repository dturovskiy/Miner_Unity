using UnityEngine;

public class LadderBehaviour : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        HeroController hero = other.GetComponent<HeroController>();
        if (hero != null)
        {
            hero.SetIsOnLadder(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        HeroController hero = other.GetComponent<HeroController>();
        if (hero != null)
        {
            hero.SetIsOnLadder(false);
        }
    }
}
