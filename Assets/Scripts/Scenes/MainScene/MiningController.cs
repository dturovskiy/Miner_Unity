using System;
using UnityEngine;

public class MiningController : MonoBehaviour
{
    //[SerializeField] TerrainController terrainController;


    private Animator animator;
    private HeroStateController stateController;
    private TileBehaviour tileBehaviour;

    private float maxMiningDistance = 0.5f;

    public Joystick miningJoystick;

    private float miningDelay = 0.4f;
    private float startTime;

    private bool isMiningStarted = false;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        stateController = GetComponent<HeroStateController>();
    }

    private void Update()
    {
        float horizontalInput = miningJoystick.Horizontal;
        float verticalInput = miningJoystick.Vertical;

        int roundedHorizontalInput = Mathf.RoundToInt(horizontalInput);
        int roundedVerticalInput = Mathf.RoundToInt(verticalInput);

        // Require a very deliberate input so normal walking doesn't hijack mining
        if (Math.Abs(horizontalInput) >= 0.8f || Math.Abs(verticalInput) >= 0.8f)
        {
            Vector2 miningDirection = new Vector2(roundedHorizontalInput, roundedVerticalInput).normalized;
            if (roundedVerticalInput == 1)
            {
                maxMiningDistance = 0.7f;
            }
            else
            {
                maxMiningDistance = 0.4f;
            }
            Vector2 miningPosition = (Vector2)transform.position + miningDirection * maxMiningDistance;

            //if (!terrainController.inCave)
            {
                CheckTile(miningPosition);
            }
        }
        else
        {
            StopMiningAnimation();
        }
    }

    private void StartMiningAnimation()
    {
        if (!isMiningStarted)
        {
            isMiningStarted = true;
            startTime = Time.time;
        }

        animator.SetBool("IsMining", true);
        animator.SetBool("IsWalking", false);
        
        // TEMPORARY: disabled state overriding for bug tracing.
        // stateController.ChangeState(HeroState.Mining);
    }

    private void StopMiningAnimation()
    {
        if (isMiningStarted)
        {
            isMiningStarted = false;
            startTime = 0f;
        }

        animator.SetBool("IsMining", false);

        // TEMPORARY: disabled state overriding for bug tracing.
        /*
        if (stateController != null && stateController.CurrentState == HeroState.Mining)
        {
            stateController.ChangeState(HeroState.Normal);
        }
        */
    }

    private void CheckTile(Vector2 targetPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        if (hitCollider != null)
        {
            GameObject tile = hitCollider.gameObject;
            tileBehaviour = tile.GetComponent<TileBehaviour>();

            if (tile.CompareTag("Edge") || tile.CompareTag("Stone") || tile.CompareTag("Cave"))
            {
                StopMiningAnimation();
                return;
            }

            StartMiningAnimation();

            if (Time.time - startTime >= miningDelay)
            {
                if (tileBehaviour != null && !tileBehaviour.IsBroken)
                {
                    tileBehaviour.HitTile(tileBehaviour);
                }
                if (tileBehaviour != null && tileBehaviour.IsBroken)
                {
                    StopMiningAnimation();
                    animator.SetBool("IsWalking", true);
                    Vector2 tileCoordinates = new Vector2(tile.transform.position.x, tile.transform.position.y);
                    
                }
            }
        }
        else
        {
            StopMiningAnimation();
            animator.SetBool("IsWalking", true);
        }
    }
}
