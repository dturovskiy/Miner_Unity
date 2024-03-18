using LitJson;
using System;
using System.IO;
using UnityEngine;

public class MiningController : MonoBehaviour
{
    //[SerializeField] TerrainController terrainController;


    private Animator animator;
    private HeroController heroController;
    private TileBehaviour tileBehaviour;
    private SaveLoadSystem saveLoadSystem;
    private float maxMiningDistance = 0.5f;

    public Joystick miningJoystick;

    private float miningDelay = 0.4f;
    private float startTime;

    private bool isMiningStarted = false;
    

    private void Awake()
    {
        animator = GetComponent<Animator>();
        heroController = GetComponent<HeroController>();
    }

    private void Update()
    {
        float horizontalInput = miningJoystick.Horizontal;
        float verticalInput = miningJoystick.Vertical;

        int roundedHorizontalInput = Mathf.RoundToInt(horizontalInput);
        int roundedVerticalInput = Mathf.RoundToInt(verticalInput);

        if (Math.Abs(horizontalInput) >= 0.5 || Math.Abs(verticalInput) >= 0.5)
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
            // Зупинка анімації майнінгу, якщо вона вже почалася
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
        heroController.SetCanMove(false);
    }

    private void StopMiningAnimation()
    {
        if (isMiningStarted)
        {
            isMiningStarted = false;
            startTime = 0f;
        }

        animator.SetBool("IsMining", false);

        heroController.SetCanMove(true);
    }

    private void CheckTile(Vector2 targetPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        // Перевірка наявності цілі
        if (hitCollider != null)
        {
            GameObject tile = hitCollider.gameObject;
            tileBehaviour = tile.GetComponent<TileBehaviour>();

            // Перевірка тегів та стану плитки
            if (tile.CompareTag("Edge") || tile.CompareTag("Stone") || tile.CompareTag("Cave")) return;

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
                    Vector2Int tileCoordinates = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x), Mathf.RoundToInt(tile.transform.position.y));
                    saveLoadSystem.SaveDestroyedBlock(tileCoordinates);

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
