using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

public class MeleeWeaponController : WeaponController
{
    public float Damage = 40f;
    public float Range = 2f;
    public float AttackRate = 1f;

    float m_LastAttackTime;

    PlayerWeaponsManager playerWeaponsManager;

    void Start()
    {
        playerWeaponsManager = GetComponentInParent<PlayerWeaponsManager>();
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
        var weaponCamera = playerWeaponsManager.WeaponCamera;
        if (Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out RaycastHit hit, Range))
        {
            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(Damage, gameObject);
            }
        }
        // Play swing animation/sound here
    }
}
