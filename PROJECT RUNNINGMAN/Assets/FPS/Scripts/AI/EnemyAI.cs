using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    // --------- PATROL / MOVEMENT ----------
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    [Tooltip("Temporarily used while swinging so they don't overshoot the target")]
    public float swingSpeed = 0.5f;
    public float waitTimeAtWaypoint = 2f;

    // --------- CHASE THROTTLING ----------
    [Header("Chase Throttling")]
    [Tooltip("Min seconds between SetDestination calls")]
    public float repathInterval = 0.2f;
    [Tooltip("Min distance delta before we repath")]
    public float repathDistance = 0.75f;
    private float _nextRepathTime = 0f;
    private Vector3 _lastDest;

    // --------- DETECTION ----------
    [Header("Detection Settings")]
    public float viewRadius = 15f;
    public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    // --------- LOS THROTTLING ----------
    [Header("LOS Throttling")]
    [Tooltip("Min seconds between LOS checks")]
    public float losCheckInterval = 0.2f;
    private float _nextLosCheck = 0f;

    // --------- ATTACK ----------
    [Header("Attack Settings")]
    public MeleeWeaponController meleeWeapon;
    public Transform headTransform;
    public float attackDistance = 2f;
    public float attackCooldown = 1f;

    private float _attackJitter;
    private float _resumeAt = -1f;
    private float _restoreSpeed = 0f;

    // --------- AGGRO ----------
    [Header("Aggro Settings")]
    public float aggroDuration = 8f;
    public bool aggroOnDamage = true;

    // --------- KNOCKBACK ----------
    [Header("Knockback Settings")]
    public float knockbackForce = 45f;
    public float knockbackDuration = 1.0f;
    public AnimationCurve knockbackDamping = AnimationCurve.EaseInOut(0, 1, 1, 1);

    private bool isKnockback = false;
    private float knockbackStartTime;
    private Vector3 knockbackDirection;
    private Transform m_Transform;

    // --------- BUMP RETREAT ----------
    [Header("Bump Retreat (Patrol Only)")]
    public float bumpRetreatDistance = 2.0f;
    public float bumpRetreatDuration = 0.8f;
    public float bumpCooldown = 0.5f;

    // --------- EVENTS ----------
    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDie;

    // --------- DEBUG ----------
    [Header("Debug")]
    public Color viewGizmoColor = Color.blue;
    public Color attackGizmoColor = Color.red;

    // --------- DROPS / DEATH ----------
    [Header("Death & Drops")]
    public GameObject DeathVfx;
    public Transform DeathVfxSpawnPoint;
    [Range(0, 1)] public float DropRate = 1f;
    public float DeathDuration = 0f;
    [SerializeField] private GameObject audiencePickupPrefab;
    [SerializeField] private GameObject corporatePickupPrefab;
    public CurrencyManager currencyManager;

    [Header("Death Sounds")]
    public float deathSoundPause = 0f;
    public AudioClip deathSoundSFX;

    [Header("Audio")]
    public AudioClip HitSFX;

    // --------- SLOW-MO ----------
    [Header("SlowMo (on last enemy death)")]
    public float slowMoDuration = 0.35f;
    [Range(0.01f, 1f)] public float slowMoScale = 0.2f;
    public bool SlowMoEnabled = true;

    // --------- ANIMATOR ----------
    [Header("Animator Params")]
    public string animParamIsJogging = "IsJogging";
    private int _hashIsJogging;

    // --------- INTERNALS ----------
    private NavMeshAgent agent;
    private Health health;
    private Transform player;
    private Animator anim;
    private AudioSource audioSource;

    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = -999f;
    private bool playerInSight = false;

    private bool isAggro = false;
    private float aggroEndTime = -1f;
    private Vector3 lastKnownPlayerPos;

    private bool isBumpRetreating = false;
    private float bumpCooldownUntil = -1f;

    private static int s_aliveCount = 0;
    private bool countedAlive = false;

    private Renderer[] _renderers;
    private bool _aiPausedByVisibility = false;

    private static bool s_slowMoActive = false;

    private bool IsDead => health != null && health.CurrentHealth <= 0;

    // -------------------- LIFECYCLE --------------------

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        m_Transform = transform;

        anim = GetComponentInChildren<Animator>(true);
        if (anim)
        {
            anim.cullingMode = AnimatorCullingMode.CullCompletely;
            _hashIsJogging = Animator.StringToHash(animParamIsJogging);
        }

        audioSource = GetComponent<AudioSource>();

        if (meleeWeapon == null)
            meleeWeapon = GetComponentInChildren<MeleeWeaponController>();

        if (headTransform == null)
            headTransform = transform.Find("Head");

        if (currencyManager == null)
            currencyManager = FindObjectOfType<CurrencyManager>();

        s_aliveCount++;
        countedAlive = true;

        _attackJitter = Random.Range(0f, 0.2f);

        _renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in _renderers)
        {
            if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
        }
    }

    private void Start()
    {
        if (health == null)
        {
            Debug.LogError("Health missing!");
            return;
        }

        health.OnDie += OnDie;
        health.OnDamaged += OnDamaged;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (player != null)
            lastKnownPlayerPos = player.position;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDie -= OnDie;
        }

        if (countedAlive)
        {
            s_aliveCount = Mathf.Max(0, s_aliveCount - 1);
            countedAlive = false;
        }
    }

    // -------------------- KNOCKBACK --------------------

    public void ApplyKnockback(Vector3 hitPoint)
    {
        if (IsDead) return;

        Vector3 dir = m_Transform.position - hitPoint;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = -m_Transform.forward;
        else
            dir.Normalize();

        knockbackDirection = dir;
        isKnockback = true;
        knockbackStartTime = Time.time;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.ResetPath();
        }

        if (anim) anim.SetTrigger("Knockback");
    }

    // -------------------- UPDATE --------------------

    private void Update()
    {
        if (IsDead)
        {
            agent.isStopped = true;
            return;
        }

        // Knockback
        if (isKnockback)
        {
            float t = (Time.time - knockbackStartTime) / Mathf.Max(0.0001f, knockbackDuration);
            if (t < 1f)
            {
                float damp = knockbackDamping.Evaluate(Mathf.Clamp01(t));
                // ✅ knockbackForce is now total displacement in units
                m_Transform.position += knockbackDirection * knockbackForce * damp * (Time.deltaTime / knockbackDuration);
            }
            else
            {
                isKnockback = false;
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.Warp(m_Transform.position);
                    agent.updatePosition = true;
                    agent.isStopped = false;
                }
            }
            return;
        }

        // Visibility gate
        bool visible = IsVisible();
        if (anim && anim.enabled != visible) anim.enabled = visible;
        _aiPausedByVisibility = !visible;
        if (_aiPausedByVisibility)
        {
            if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        // Resume after swing
        if (_resumeAt > 0f && Time.time >= _resumeAt)
        {
            agent.isStopped = false;
            agent.speed = _restoreSpeed > 0f ? _restoreSpeed : runSpeed;
            _resumeAt = -1f;
        }

        DetectPlayerThrottled();

        if (playerInSight && player != null)
        {
            isAggro = true;
            aggroEndTime = Time.time + aggroDuration;
            lastKnownPlayerPos = player.position;
        }
        if (isAggro && Time.time > aggroEndTime)
            isAggro = false;

        if (isBumpRetreating) return;

        if (isAggro || playerInSight)
        {
            ChasePlayerThrottled();
            TryAttackPlayerThrottled();
            anim?.SetBool("IsJogging", true);
        }
        else
        {
            Patrol();
            anim?.SetBool("IsJogging", false);
        }
    }

    private bool IsVisible()
    {
        if (_renderers == null || _renderers.Length == 0) return true;
        foreach (var r in _renderers)
            if (r && r.isVisible) return true;
        return false;
    }

    // -------------------- DAMAGE --------------------

    private void OnDamaged(float damage, GameObject damageSource)
    {
        // Debug.Log($"{gameObject.name} took {damage} from {damageSource}");

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
                _lastDest = lastKnownPlayerPos;
                _nextRepathTime = Time.time + repathInterval;
            }
        }

        // Use exact hit point if Health recorded it
        if (health.LastHitPoint != Vector3.zero)
            ApplyKnockback(health.LastHitPoint);

        DetectPlayerThrottled();
        onDamaged?.Invoke();
    }

    // -------------------- SENSING --------------------

    private void DetectPlayerThrottled()
    {
        if (player == null) { playerInSight = false; return; }
        if (Time.time < _nextLosCheck) return;
        _nextLosCheck = Time.time + losCheckInterval;

        Vector3 dir = (player.position - transform.position);
        float dist = dir.magnitude;
        playerInSight = false;

        if (dist <= viewRadius)
        {
            Vector3 dirNorm = dir / (dist > 0.0001f ? dist : 1f);
            if (Vector3.Angle(transform.forward, dirNorm) <= viewAngle * 0.5f)
            {
                if (!Physics.Raycast(transform.position, dirNorm, dist, obstacleMask))
                {
                    playerInSight = true;
                    if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
                }
            }
        }
    }

    // -------------------- LOCOMOTION --------------------

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

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

    private void ChasePlayerThrottled()
    {
        Vector3 dest = (player != null) ? player.position : lastKnownPlayerPos;
        if (player != null) lastKnownPlayerPos = dest;

        if (Time.time >= _nextRepathTime || (dest - _lastDest).sqrMagnitude >= repathDistance * repathDistance)
        {
            agent.speed = runSpeed;
            agent.isStopped = false;
            agent.SetDestination(dest);
            _lastDest = dest;
            _nextRepathTime = Time.time + repathInterval;
        }
    }

    // -------------------- ATTACK --------------------

    private void TryAttackPlayerThrottled()
    {
        if (player == null || meleeWeapon == null || headTransform == null) return;
        if (Time.time < lastAttackTime + attackCooldown + _attackJitter) return;

        float distance = Vector3.Distance(headTransform.position, player.position);
        if (distance > attackDistance) return;

        lastAttackTime = Time.time;

        _restoreSpeed = agent.speed;
        agent.speed = swingSpeed;
        agent.isStopped = true;

        Vector3 targetPoint = player.position + Vector3.up * 1.0f;
        Vector3 dirToPlayer = (targetPoint - headTransform.position).normalized;

        if (Physics.Raycast(headTransform.position, dirToPlayer, out RaycastHit hit, meleeWeapon.Range))
        {
            bool blocked = false;

            var targetWeapons = hit.collider.GetComponentInParent<PlayerWeaponsManager>();
            if (targetWeapons != null)
            {
                var active = targetWeapons.GetActiveWeapon();
                var targetMelee = active as MeleeWeaponController;
                if (targetMelee != null && targetMelee.TryBlockHit())
                {
                    blocked = true;
                }
            }

            if (!blocked)
            {
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null)
                {
                    h.TakeDamage(meleeWeapon.Damage, gameObject);
                    if (HitSFX && audioSource) audioSource.PlayOneShot(HitSFX);
                }
            }
        }

        meleeWeapon.PerformAttack(headTransform, dirToPlayer);
        if (anim) anim.SetTrigger("Strike");

        _resumeAt = Time.time + 0.3f;
    }

    // -------------------- BUMP RETREAT --------------------

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
            DetectPlayerThrottled();
            ChasePlayerThrottled();
            TryAttackPlayerThrottled();
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

    // -------------------- DEATH --------------------

    private void OnDie()
    {
        if (countedAlive)
        {
            countedAlive = false;
            s_aliveCount = Mathf.Max(0, s_aliveCount - 1);
        }

        if (DeathVfx != null && DeathVfxSpawnPoint != null)
        {
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        if (deathSoundSFX != null && audioSource != null)
        {
            StartCoroutine(DeathSoundProc());
        }

        if (audiencePickupPrefab != null && Random.value <= DropRate)
            Instantiate(audiencePickupPrefab, transform.position, Quaternion.identity);

        if (corporatePickupPrefab != null && currencyManager != null && currencyManager.hasSponser)
        {
            float corporateChance = Mathf.Clamp01(DropRate * 0.5f);
            if (Random.value <= corporateChance)
                Instantiate(corporatePickupPrefab, transform.position, Quaternion.identity);
        }

        if (SlowMoEnabled && s_aliveCount == 0)
        {
            StartCoroutine(PlayLastKillSlowMoSafe());
        }

        onDie?.Invoke();
        Destroy(gameObject, DeathDuration);
    }

    private IEnumerator DeathSoundProc()
    {
        if (audioSource != null && deathSoundSFX != null)
        {
            if (deathSoundPause > 0f)
                yield return new WaitForSeconds(deathSoundPause);
            audioSource.PlayOneShot(deathSoundSFX);
        }
    }

    private IEnumerator PlayLastKillSlowMoSafe()
    {
        if (s_slowMoActive) yield break;
        s_slowMoActive = true;

        float oldScale = Time.timeScale;
        float oldFixed = Time.fixedDeltaTime;
        try
        {
            Time.timeScale = Mathf.Clamp(slowMoScale, 0.01f, 1f);
            Time.fixedDeltaTime = oldFixed * Time.timeScale;
            yield return new WaitForSecondsRealtime(slowMoDuration);
        }
        finally
        {
            Time.timeScale = oldScale;
            Time.fixedDeltaTime = oldFixed;
            s_slowMoActive = false;
        }
    }

    // -------------------- GIZMOS --------------------

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