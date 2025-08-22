using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.FPS.Game;

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

    [Header("Death & Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDie;

    [Header("Debug")]
    public Color viewGizmoColor = Color.blue;
    public Color attackGizmoColor = Color.red;

    private NavMeshAgent agent;
    private Health health;
    private Transform player;
    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = -999f;
    private bool playerInSight = false;
    public GameObject DeathVfx;
    public Transform DeathVfxSpawnPoint;
    public GameObject LootPrefab;
    [Range(0, 1)]
    public float DropRate = 1f;
    public float DeathDuration = 0f; // Delay before destroying enemy


    private bool IsDead => health.CurrentHealth <= 0;

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
    }



    void Update()
    {
        

        if (health.CurrentHealth <= 0)
        {
            agent.isStopped = true;
            return;
        }

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


    void DetectPlayer()
    {
        if (player == null) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);
        playerInSight = false;

        if (distance <= viewRadius && Vector3.Angle(transform.forward, dirToPlayer) <= viewAngle / 2)
        {
            if (!Physics.Raycast(transform.position, dirToPlayer, distance, obstacleMask))
            {
                playerInSight = true;
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
        if (player == null) return;

        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void TryAttackPlayer()
    {
        if (player == null || meleeWeapon == null) return;

        float distance = Vector3.Distance(headTransform.position, player.position);
        if (distance <= attackDistance && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            // Aim slightly higher (toward chest height)
            Vector3 targetPoint = player.position + Vector3.up * 1.0f;
            Vector3 dirToPlayer = (targetPoint - headTransform.position).normalized;

            // Debug ray so you can see in Scene view where it's aiming
            Debug.DrawRay(headTransform.position, dirToPlayer * meleeWeapon.Range, Color.red, 1f);
            Debug.Log($"Enemy attacking player. Distance: {distance}, Dir: {dirToPlayer}");

            if (Physics.Raycast(headTransform.position, dirToPlayer, out RaycastHit hit, meleeWeapon.Range))
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(meleeWeapon.Damage, gameObject);
                }
            }

            // Perform the melee attack animation/audio/etc
            meleeWeapon.PerformAttack(headTransform, dirToPlayer);
        }
    }



    public void OnDamaged(float damage)
    {
        onDamaged?.Invoke();
       
    }

    private void OnDie()
    {
        Debug.Log("Dead");
        // Play death VFX
        if (DeathVfx != null && DeathVfxSpawnPoint != null)
        {
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f); // cleanup
        }

        // Drop loot
        if (LootPrefab != null && Random.value <= DropRate)
            Instantiate(LootPrefab, transform.position, Quaternion.identity);

        // Destroy enemy after a delay
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
