using UnityEngine;

[RequireComponent(typeof(HeroStateController), typeof(HeroInputReader))]
public class LadderZoneDetector : MonoBehaviour
{
    private HeroStateController heroState;
    private HeroInputReader inputReader;
    private int ladderContacts = 0;

    private void Awake()
    {
        heroState = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder") || other.GetComponent<LadderBehaviour>() != null)
        {
            ladderContacts++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder") || other.GetComponent<LadderBehaviour>() != null)
        {
            ladderContacts--;
            if (ladderContacts < 0) ladderContacts = 0;
            
            if (ladderContacts == 0 && heroState.CurrentState == HeroState.Climbing)
            {
                heroState.ChangeState(HeroState.Normal);
            }
        }
    }

    private void Update()
    {
        bool isInsideLadderZone = ladderContacts > 0;

        if (isInsideLadderZone && heroState.CurrentState == HeroState.Normal)
        {
            if (Mathf.Abs(inputReader.Vertical) > 0.1f)
            {
                heroState.ChangeState(HeroState.Climbing);
            }
        }
    }
}
