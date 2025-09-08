using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;


[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
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

  
    [Header("Aggro Settings")]
    public float aggroDuration = 8f;
    public bool aggroOnDamage = true;

    [Header("Bump Retreat (Patrol Only)")]
    //public string enemyTag = "Enemy";
    public float bumpRetreatDistance = 2.0f;
    public float bumpRetreatDuration = 0.8f;
    public float bumpCooldown = 0.5f;

    [Header("Death & Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDie;

    [Header("Debug")]
    public Color viewGizmoColor = Color.blue;
    public Color attackGizmoColor = Color.red;

    private NavMeshAgent agent;
    public Health health;
    private Transform player;
    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = -999f;
    private bool playerInSight = false;

    private bool isAggro = false;
    private float aggroEndTime = -1f;
    private Vector3 lastKnownPlayerPos;

    // Retreat state
    private bool isBumpRetreating = false;
    private float bumpCooldownUntil = -1f;

    public GameObject DeathVfx;
    public Transform DeathVfxSpawnPoint;
    public GameObject LootPrefab;
    [Range(0, 1)] public float DropRate = 1f;
    public float DeathDuration = 0f;

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

        if (currencyManager == null)
            currencyManager = FindObjectOfType<CurrencyManager>();

        if (player != null)
            lastKnownPlayerPos = player.position;
    }

    void Update()
    {
        if (health.CurrentHealth <= 0)
        {
            agent.isStopped = true;
            return;
        }

        DetectPlayer();

        if (playerInSight && player != null)
        {
            isAggro = true;
            aggroEndTime = Time.time + aggroDuration;
            lastKnownPlayerPos = player.position;
        }

        if (isAggro && Time.time > aggroEndTime)
            isAggro = false;

        // Priority: retreat > chase > patrol
        if (isBumpRetreating) return;

        else if (isAggro || playerInSight)
        {
            ChasePlayer();
            TryAttackPlayer(); 
        }

        else
        {
            Patrol();
        }
    }

    void DetectPlayer()
    {
        if (player == null) { playerInSight = false; return; }

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);
        playerInSight = false;

        if (distance <= viewRadius && Vector3.Angle(transform.forward, dirToPlayer) <= viewAngle / 2)
        {
            if (!Physics.Raycast(transform.position, dirToPlayer, distance, obstacleMask))
                playerInSight = true;
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
        Vector3 dest = (player != null) ? player.position : lastKnownPlayerPos;
        if (player != null) lastKnownPlayerPos = dest;

        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.SetDestination(dest);
    }

    void TryAttackPlayer()
    {
        if (player == null || meleeWeapon == null) return;

        float distance = Vector3.Distance(headTransform.position, player.position);
        if (distance <= attackDistance && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            Vector3 targetPoint = player.position + Vector3.up * 1.0f;
            Vector3 dirToPlayer = (targetPoint - headTransform.position).normalized;

            Debug.DrawRay(headTransform.position, dirToPlayer * meleeWeapon.Range, Color.red, 1f);

            bool blocked = false;

            if (Physics.Raycast(headTransform.position, dirToPlayer, out RaycastHit hit, meleeWeapon.Range))
            {
                // 1) Ask the player's active weapon (if melee) to block
                var targetWeapons = hit.collider.GetComponentInParent<PlayerWeaponsManager>();
                if (targetWeapons != null)
                {
                    var active = targetWeapons.GetActiveWeapon();
                    var targetMelee = active as MeleeWeaponController;
                    if (targetMelee != null && targetMelee.TryBlockHit())
                    {
                        Debug.Log("Blocked hit!");
                        blocked = true; // don't return; still play the swing below
                    }
                }

                // 2) Apply damage here if NOT blocked
                if (!blocked)
                {
                    var h = hit.collider.GetComponentInParent<Health>();
                    if (h != null)
                    {
                        
                        h.TakeDamage(meleeWeapon.Damage, gameObject);
                    }
                }
            }

           
            // 3) Always play the melee swing
            meleeWeapon.PerformAttack(headTransform, dirToPlayer);
        }
    }

   



    public void OnDamaged(float damage)
    {
        if (aggroOnDamage)
        {
            isAggro = true;
            aggroEndTime = Time.time + aggroDuration;

            if (player != null)
            {
                lastKnownPlayerPos = player.position;
                agent.speed = runSpeed;
                agent.isStopped = false;
                agent.SetDestination(lastKnownPlayerPos);
            }
        }

        DetectPlayer();
        onDamaged?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            TryStartBumpRetreat(collision.GetContact(0).normal);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Vector3 away = (transform.position - other.transform.position).normalized;
            TryStartBumpRetreat(away);
        }

        if (other.CompareTag("Player"))
        {
            DetectPlayer();
            ChasePlayer();
            TryAttackPlayer();
        }
    }

    private void TryStartBumpRetreat(Vector3 awayNormal)
    {
        if (isAggro || playerInSight || isBumpRetreating) return;
        if (Time.time < bumpCooldownUntil) return;

        StartCoroutine(BumpRetreatRoutine(awayNormal));
    }

    private IEnumerator BumpRetreatRoutine(Vector3 awayNormal)
    {
        isBumpRetreating = true;
        bumpCooldownUntil = Time.time + bumpCooldown;

        if (awayNormal.sqrMagnitude < 0.01f) awayNormal = -transform.forward;
        Vector3 retreatPos = transform.position + awayNormal.normalized * bumpRetreatDistance;

        if (NavMesh.SamplePosition(retreatPos, out var hit, bumpRetreatDistance + 1f, NavMesh.AllAreas))
        {
            agent.speed = walkSpeed;
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        yield return new WaitForSeconds(bumpRetreatDuration);
        isBumpRetreating = false;
    }
    

    private void OnDie()
    {
        if (DeathVfx != null && DeathVfxSpawnPoint != null)
        {
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        if (audiencePickupPrefab != null && Random.value <= DropRate)
            Instantiate(audiencePickupPrefab, transform.position, Quaternion.identity);

        // Drop Corporate favor half of the time
        if (corporatePickupPrefab != null && currencyManager != null && currencyManager.hasSponser)
        {
            float corporateChance = Mathf.Clamp01(DropRate * 0.5f);
            if (Random.value <= corporateChance)
                Instantiate(corporatePickupPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, DeathDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = viewGizmoColor;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        if (headTransform != null)
        {
            Gizmos.color = attackGizmoColor;
            Gizmos.DrawWireSphere(headTransform.position, attackDistance);
        }
    }
}
