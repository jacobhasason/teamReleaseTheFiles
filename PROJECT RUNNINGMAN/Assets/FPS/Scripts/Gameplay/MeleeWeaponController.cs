using System.Collections;
using System.Diagnostics;
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


    float m_LastAttackTime;

    

    [Header("Sound and Effects")]

    [Tooltip("Prefab of the hit flash")]
    public GameObject HitFlashPrefab;

    [Tooltip("Sound played when swinging")]
    public AudioClip SwingSfx;

    [Tooltip("Percussive sound played when a hit is made")]
    public AudioClip HitSoundSfx;

    [Tooltip("Tuned sound played when a hit is made")]
    public AudioClip ToneSoundSfx;
    
    public AudioMixerGroup TonedSounds;
    AudioSource MeleeAudioSource;

    PlayerWeaponsManager playerWeaponsManager;

    void Start()
    {
        playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();

        if(playerWeaponsManager == null)
        Debug.LogError("PlayerWeaponsManager not found in parent!");

        if (playerWeaponsManager?.WeaponCamera == null)
            Debug.LogError("WeaponCamera not assigned in PlayerWeaponsManager!");
        MeleeAudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, MeleeWeaponController>(MeleeAudioSource, this,
            gameObject);
    }


    public override bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        if (inputDown && Time.time - m_LastAttackTime > 1f / AttackRate)
        {
            m_LastAttackTime = Time.time;
            PerformAttack();
            return true;
        }
        return false;
    }

    void PerformAttack()
    {
        WeaponAnimator?.SetTrigger("Swing");

        Debug.Log("Attack!");
        MeleeAudioSource.PlayOneShot(SwingSfx);
        var weaponCamera = playerWeaponsManager.WeaponCamera;
        if (Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out RaycastHit hit, Range))
        {
            StartCoroutine(SwingProc());
            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(Damage, gameObject);
            }
            var kb_health = hit.collider.GetComponentInParent<KBHealth>();
            if (kb_health != null)
            {
                var kb_tune = 1f;
                var kb_vol = 1f;

                if (KBDamage >= kb_health.CurrentHealth / 3)
                {
                    kb_tune = .8875f;
                    kb_vol = .5f;
                }
                else if (KBDamage >= kb_health.CurrentHealth/2)
                {
                    kb_tune = .9402f;
                    kb_vol = .75f;
                }
                else if (KBDamage >= kb_health.CurrentHealth)
                {
                    kb_tune = 1f;
                    kb_vol = 1f;
                }
                kb_health.TakeDamage(Damage, gameObject);
                StartCoroutine(TunedSwingProc(kb_tune, kb_vol));
            }
            
        }
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
