using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float MovementSpeed = 10f;
    [SerializeField] private float secondaryHandSpeed = 5f; // Controls how fast secondary hand moves
    [SerializeField] private float secondaryHandSpeedWhileAttacking = 90f;
    [SerializeField] private float followingDistance = 6f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform secondHand;

    private Rigidbody2D rigidBody;
    private Vector2 facingDirection = Vector2.right; // Start facing right
    private bool attacking = false;
    private bool facingDir = true;
    public bool FacingDir => facingDir;
    private Vector2 secondCurrentPos, secondTargetPos;

    bool secondHandReadyToMove = false;
    private Vector2 mainHandPosition;
    private float footstepCounter;

    private float upgradeTimer = 8f;
    private int upgradeIndex = 0;

    private const float FOOTSTEP_INTERVAL = 0.75f;
    private const float POSITION_LERP_SPEED = 5f;

    [HideInInspector] public bool CanMove = true;
    [HideInInspector] public bool Moving;
    [HideInInspector] public Vector2 PlayerPosition;
    [HideInInspector] public Vector2 currentPos, targetPos;
    public static PlayerMovement Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CanMove = true;
        rigidBody = GetComponent<Rigidbody2D>();
        RegisterEventHandlers();

        // Initialize second hand position
        UpdateSecondHandTarget(Vector2.right);
    }

    private void RegisterEventHandlers()
    {
        PlayerAttack.OnAttackInitiate += AttackStart;
        PlayerAttack.OnAttackHalt += AttackEnd;
    }

    private void Update()
    {
        footstepCounter += Time.deltaTime;
        UpdatePositions();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleTriggerCollision(collision);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePhysicalCollision(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        HandleTriggerExit(collision);
    }

    private void HandlePhysicalCollision(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            HandleEnemyCollision();
        }
    }

    private void HandleTriggerExit(Collider2D collision)
    {
        if (collision.CompareTag("Selection"))
        {
            CancelUpgradeSelection();
        }
    }

    private void HandleTriggerCollision(Collider2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Selection":
                HandleUpgradeSelection(collision);
                break;
            case "Enemy":
                HandleEnemyCollision();
                break;
            case "Coin":
                HandleCoinCollection(collision);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (attacking)
        {
            // When attacking, move secondary hand with WASD
            Vector2 movement = GetMovementInput();
            if(secondHandReadyToMove)
                MoveSecondaryHand(movement);
            rigidBody.velocity = Vector2.zero; // Keep primary hand still
        }
        else
        {
            // Normal movement
            HandleMovement();
        }
    }

    private void UpdatePositions()
    {
        PlayerPosition = transform.position;

        // Smoothly move secondary hand to target
        secondHand.position = secondCurrentPos = Vector3.Slerp(
            secondCurrentPos,
            secondTargetPos,
            Time.deltaTime * (attacking ? secondaryHandSpeedWhileAttacking : secondaryHandSpeed)
        ) ;
    }

    private void MoveSecondaryHand(Vector2 movement)
    {
        if (movement.magnitude > 0)
        {
            Vector2 newPos = (Vector2)secondHand.position + (movement * MovementSpeed * Time.fixedDeltaTime);

            // Limit distance from main hand
            Vector2 toMain = (Vector2)transform.position - newPos;
            if (toMain.magnitude > followingDistance)
            {
                newPos = (Vector2)transform.position - toMain.normalized * followingDistance;
            }

            secondTargetPos = newPos;
        }
    }

    private void HandleMovement()
    {
        if (!CanMove) return;

        Vector2 movement = GetMovementInput();

        if (movement.magnitude > 0)
        {
            // Update facing direction when moving
            facingDirection = movement.normalized;
            Moving = true;

            if (footstepCounter > FOOTSTEP_INTERVAL)
            {
                SoundManager.Instance.PlaySoundEffect("player_walk");
                footstepCounter = 0;
            }
        }
        else
        {
            Moving = false;
        }

        // Apply movement
        var speedMult = CalculateSpeedMultiplier();
        rigidBody.velocity = movement * MovementSpeed * speedMult;

        // Update second hand target based on raycast
        UpdateSecondHandTarget(movement);
    }

    private Vector2 GetMovementInput()
    {
        var movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (GameManager.Instance.upgradeList.ContainsKey(UpgradeType.Confusion))
        {
            movement *= -Vector2.one;
        }

        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }

        return movement;
    }

    private float CalculateSpeedMultiplier()
    {
        var gm = GameManager.Instance;
        var healthRatio = Mathf.Clamp01(gm.GetHealthRatio() * 2);
        return healthRatio * gm.GetPowerMult(UpgradeType.Lightning, 1.5f);
    }

    private void UpdateSecondHandTarget(Vector2 movement)
    {
        Vector2 rayDirection;

        if (movement.magnitude > 0)
        {
            rayDirection = -movement.normalized; // Opposite of movement
        }
        else
        {
            rayDirection = -facingDirection; // Use stored facing direction
        }

        int layerMask = ~((1 << 6) | (1 << 12));
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, followingDistance, layerMask);

        if (hit.collider != null)
        {
            secondTargetPos = hit.point;
        }
        else
        {
            secondTargetPos = (Vector2)transform.position + (rayDirection * followingDistance);
        }
    }

    //private void AttackStart()
    //{
    //    attacking = true;
    //    mainHandPosition = transform.position;
    //}

    private void AttackStart()
    {
        secondHandReadyToMove = false;
        StartCoroutine(nameof(WaitForSecondHandReadyToMove));

        attacking = true;
        mainHandPosition = transform.position;

        Vector2 direction = (secondCurrentPos - (Vector2)transform.position).normalized;
        secondTargetPos = (Vector2)transform.position + direction;
    }

    private IEnumerator WaitForSecondHandReadyToMove()
    {
        while (Vector2.Distance(mainHandPosition, secondHand.position) > 1.5f)
        {
            //Debug.Log((Vector2.Distance(mainHandPosition, secondHand.position) > 1.5f));
            yield return null;
        }
        secondHandReadyToMove = true;
    }

    private void AttackEnd()
    {
        StopCoroutine(nameof(WaitForSecondHandReadyToMove));

        attacking = false;
        // Reset second hand position
        UpdateSecondHandTarget(facingDirection);
    }

    public Vector2 GetDirectionOfPrimaryHand()
    {
        if (attacking)
            return ((Vector2)secondHand.position - mainHandPosition).normalized;
        return -facingDirection;
    }

    public Vector2 GetDirectionToPrimaryHand()
    {
        var dir = (Vector2)secondHand.position - (Vector2)transform.position;
        return dir.normalized;
    }

    private void CancelUpgradeSelection()
    {
        StopCoroutine(nameof(UpgradeCountdown));
        upgradeTimer = 8f;
    }

    private void HandleUpgradeSelection(Collider2D collision)
    {
        upgradeIndex = int.Parse(collision.gameObject.name);
        StartCoroutine(nameof(UpgradeCountdown));
    }

    private void HandleEnemyCollision()
    {
        Debug.Log("Player damaged");
        GameManager.Instance.UpdateHealth();
    }

    private void HandleCoinCollection(Collider2D collision)
    {
        collision.GetComponent<Coin>().ClaimCoin();
        Debug.Log("Picked up coin.");
    }

    private IEnumerator UpgradeCountdown()
    {
        while (upgradeTimer > 0)
        {
            upgradeTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                upgradeTimer = 0;
            }
            yield return null;
        }
        UpgradeManager.Instance.ClaimUpgrade(upgradeIndex);
    }
}