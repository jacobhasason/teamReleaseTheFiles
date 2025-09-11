using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyFBAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float walkSpeed = 3.5f;
    public float chargeSpeed = 8.0f;
    public float runSpeed = 6f;
    public float swingSpeed = 0.5f;
    public float waitTimeAtWaypoint = 2f;

    [Header("Detection Settings")]
    public float viewRadius = 15f;
    public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Attack Settings")]
    public MeleeWeaponController meleeWeapon;
    public Transform headTransform;      // Raycast origin for attack
    public float attackDistance = 2f;
    public float attackCooldown = 1f;
    public float chargeDamage = 5f;
    public float jogDamage = 2f;
    public float attack = 2f;

    public float chargeCooldown = 5f;
    public float chargeRundown = 3f;
    private float waitCCooldown = 0.0f;
    private float waitCRundown = 0.0f;
    [Header("Death & Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDie;

    [Header("Debug")]
    public Color viewGizmoColor = Color.blue;
    public Color attackGizmoColor = Color.red;

    private NavMeshAgent agent;
    private Health health;
    private Transform player;
    private Vector3 chargeDestination;
    private Animator EnemyAnimator;
    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = -999f;
    private bool playerInSight = false;
    private bool chargingPlayer = false;
    public GameObject DeathVfx;
    public Transform DeathVfxSpawnPoint;
    public GameObject LootPrefab;
    [Range(0, 1)]
    public float DropRate = 1f;
    public float DeathDuration = 0f; // Delay before destroying enemy

    [Header("Drops")]
    [SerializeField] private GameObject audiencePickupPrefab;
    [SerializeField] private GameObject corporatePickupPrefab;


    private bool IsDead => health.CurrentHealth <= 0;

    public CurrencyManager currencyManager;

    [Header("Audio")]
    public AudioClip HitSFX;
    private AudioSource audioSource;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        EnemyAnimator = GetComponent<Animator>();

        if (health == null)
        {
            Debug.LogError("Health missing!");
            return;
        }

        health.OnDie += OnDie;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (meleeWeapon == null)
            meleeWeapon = GetComponentInChildren<MeleeWeaponController>();

        if (headTransform == null)
            headTransform = transform.Find("Head");

        // Ensure we have a CurrencyManager
    if (currencyManager == null)
            currencyManager = FindObjectOfType<CurrencyManager>();
    }



    void Update()
    {
        

        if (health.CurrentHealth <= 0)
        {
            agent.isStopped = true;
            return;
        }
        if (chargingPlayer)
        {
            ChargePlayer();
            TryAttackPlayer();
        }
        else
        {
            DetectPlayer();
            if (playerInSight)
            {
                ChasePlayer();
                TryAttackPlayer();
            }
            else
            {
                Patrol();
            }
        }
    }


    void DetectPlayer()
    {
        if (player == null) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);
        playerInSight = false;
        if (distance > 2.5f * viewRadius && Physics.Raycast(headTransform.position, dirToPlayer, out RaycastHit hit, 2f * attackDistance))
        {
            EnemyAnimator?.SetTrigger("GoalIdle");
            
        }
        if (distance <= viewRadius && Vector3.Angle(transform.forward, dirToPlayer) <= viewAngle / 2)
        {
            if (!Physics.Raycast(transform.position, dirToPlayer, distance, obstacleMask))
            {
                playerInSight = true;
                EnemyAnimator?.SetTrigger("Jog");
            }
        }
    }

    void Patrol()
    {
        if (waypoints.Length == 0) return;

        agent.speed = walkSpeed;
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (waitTimer <= 0f)
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypoint].position);
                waitTimer = waitTimeAtWaypoint;
            }
            else
            {
                agent.isStopped = true;
                waitTimer -= Time.deltaTime;
            }
        }
    }

    void ChasePlayer()
    {

        if (player == null || EnemyAnimator == null) return;
        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        if (waitCCooldown >= chargeCooldown)
        {
            waitCRundown = 0.0f;
            chargingPlayer = true;
            chargeDestination = player.position;
            EnemyAnimator?.SetTrigger("FBTackle");
        }
    }
    void ChargePlayer()
    {
        if (player == null || EnemyAnimator == null) return;
        agent.speed = chargeSpeed;
        agent.isStopped = false;
        agent.SetDestination(chargeDestination);
        if (waitCRundown >= chargeRundown)
        {
            waitCCooldown = 0.0f;
            chargingPlayer = false;
            EnemyAnimator?.SetTrigger("Jog");
        }
    }

    void TryAttackPlayer()
    {
        if (player == null || EnemyAnimator == null) return;

        float distance = Vector3.Distance(headTransform.position, player.position);
        if (distance <= attackDistance && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            agent.speed = swingSpeed;
            agent.isStopped = true;
            // Aim slightly higher (toward chest height)
            Vector3 targetPoint = player.position + Vector3.up * 1.0f;
            Vector3 dirToPlayer = (targetPoint - headTransform.position).normalized;

            // Debug ray so you can see in Scene view where it's aiming
            Debug.DrawRay(headTransform.position, dirToPlayer * attackDistance, Color.red, 1f);
            Debug.Log($"Enemy attacking player. Distance: {distance}, Dir: {dirToPlayer}");

            if (Physics.Raycast(headTransform.position, dirToPlayer, out RaycastHit hit, attackDistance))
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    if (chargingPlayer)
                    {
                        health.TakeDamage(chargeDamage, gameObject);
                    }
                    else
                    {
                        health.TakeDamage(jogDamage, gameObject);
                    }
                }
            }

            
        }
    }



    public void OnDamaged(float damage)
    {
        DetectPlayer();
        TryAttackPlayer();
        onDamaged?.Invoke();
       
    }

    private void OnDie()
    {
        // VFX (unchanged)
        if (DeathVfx != null && DeathVfxSpawnPoint != null)
        {
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // Audience Favor drop
        if (audiencePickupPrefab != null && Random.value <= DropRate)
        {
            Instantiate(audiencePickupPrefab, transform.position, Quaternion.identity);
        }

        // Corporate Favor drop (half as often), only if sponsorship active
        if (corporatePickupPrefab != null && currencyManager != null && currencyManager.hasSponser)
        {
            float corporateChance = Mathf.Clamp01(DropRate * 0.5f); // half the rate
            if (Random.value <= corporateChance)
            {
                Instantiate(corporatePickupPrefab, transform.position, Quaternion.identity);
            }
        }

        Destroy(gameObject, DeathDuration);
    }


    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = viewGizmoColor;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Attack radius
        if (headTransform != null)
        {
            Gizmos.color = attackGizmoColor;
            Gizmos.DrawWireSphere(headTransform.position, attackDistance);
        }
    }
}
