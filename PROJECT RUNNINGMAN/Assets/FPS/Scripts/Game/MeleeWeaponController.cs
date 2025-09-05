using System.Collections;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.Audio;
using Debug = UnityEngine.Debug;

public class MeleeWeaponController : WeaponController
{
    public float Damage = 40f;
    public float KBDamage = 1f;
    public float Range = 2f;
    public float AttackRate = 1f;
    public float HitSoundPause = .08f;

    [Header("Modifiers")]
    public float AdditionalDamage = 0f;

    float m_LastAttackTime;

    [Header("Sound and Effects")]
    public GameObject HitFlashPrefab;
    public AudioClip SwingSfx;
    public AudioClip HitSoundSfx;
    public AudioClip ToneSoundSfx;
    public AudioMixerGroup TonedSounds;

    
    [Tooltip("Hold right mouse (aim) to block.")]
    public bool IsBlocking { get; private set; }

    [Range(0f, 1f)]
    public float BlockChance = 0.66f; // 66% of hits are blocked

    private PlayerInputHandler _input;

    AudioSource MeleeAudioSource;
    PlayerWeaponsManager playerWeaponsManager;




    void Start()
    {
        playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();

        if (playerWeaponsManager == null)
            Debug.LogError("PlayerWeaponsManager not found in parent!");
        if (playerWeaponsManager?.WeaponCamera == null)
            Debug.LogError("WeaponCamera not assigned in PlayerWeaponsManager!");

        playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        _input = FindObjectOfType<PlayerInputHandler>(); // grabs the input system

        MeleeAudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, MeleeWeaponController>(
            MeleeAudioSource, this, gameObject);
    }



    void Update()
    {
        if (_input != null)
        {
            IsBlocking = _input.GetAimInputHeld(); // true while right click is held
        }
    }


    public override bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        if ((inputDown || inputHeld) && Time.time - m_LastAttackTime > 1f / AttackRate)
        {
            m_LastAttackTime = Time.time;
            PerformAttack(); // default player attack
            return true;
        }
        return false;
    }

    /// <summary>
    /// Perform a melee attack. Pass in a Transform for the origin (player camera or enemy head).
    /// </summary>
    // Old-style call (for player input or legacy calls)
    public void PerformAttack()
    {
        if (playerWeaponsManager == null) return;

        Vector3 direction = playerWeaponsManager.WeaponCamera.transform.forward;
        Transform origin = playerWeaponsManager.WeaponCamera.transform;
        PerformAttack(origin, direction);
    }

    // New method (for enemies)
    public void PerformAttack(Transform attackOrigin, Vector3 direction)
    {
        float finalDamage = Damage + AdditionalDamage;
        WeaponAnimator?.SetTrigger("Swing");
        MeleeAudioSource.PlayOneShot(SwingSfx);

        if (Physics.Raycast(attackOrigin.position, direction, out RaycastHit hit, Range))
        {
            StartCoroutine(SwingProc());

            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
                health.TakeDamage(finalDamage, gameObject);

            var kb_health = hit.collider.GetComponentInParent<KBHealth>();
            if (kb_health != null)
            {
                kb_health.TakeDamage(KBDamage, gameObject);
                StartCoroutine(TunedSwingProc(1f, 1f));
            }
        }
    }

    // Called by attackers when applying damage to the player.
    // Returns true if the hit was blocked and should be ignored.
    public bool TryBlockHit()
    {
        if (IsBlocking)
        {
            // 66% chance to succeed
            if (Random.value < BlockChance)
            {
                Debug.Log("Attack blocked!");
                return true; // muted damage
            }
        }
        return false; // not blocked, apply damage normally
    }




    private IEnumerator SwingProc()
    {
        yield return new WaitForSeconds(HitSoundPause);
        MeleeAudioSource.PlayOneShot(HitSoundSfx);
    }

    private IEnumerator TunedSwingProc(float KBTune, float KBVol)
    {
        yield return new WaitForSeconds(HitSoundPause + .02f);
        MeleeAudioSource.pitch = KBTune;
        MeleeAudioSource.volume = KBVol;
        MeleeAudioSource.PlayOneShot(ToneSoundSfx);
    }
}
