using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class WeaponPickup : Pickup
    {
        [Tooltip("Weapon prefab that will be added to the player when picked")]
        public WeaponController WeaponPrefab;

        private bool m_HasBeenPickedUp = false;
        private Collider m_Collider;

    protected override void Start()
    {
        base.Start();
        m_Collider = GetComponent<Collider>();
    }
    protected override void OnPicked(PlayerCharacterController playerController)
    {
            Debug.Log("WeaponPickup OnPicked called");

            if (m_HasBeenPickedUp)
            {
                Debug.Log("Already picked up, returning");
                return;
            }

            m_HasBeenPickedUp = true;

            if (m_Collider != null)
            {
                m_Collider.enabled = false;
                Debug.Log("Collider disabled");
            }

            var weaponsManager = playerController.GetComponent<PlayerWeaponsManager>();
            Debug.Log("WeaponsManager found: " + (weaponsManager != null));
            Debug.Log("WeaponPrefab assigned: " + (WeaponPrefab != null));

            if (weaponsManager && WeaponPrefab != null)
            {
                bool added = weaponsManager.AddWeapon(WeaponPrefab);
                Debug.Log("AddWeapon returned: " + added);

                if (added)
                {
                    var newWeaponInstance = weaponsManager.HasWeapon(WeaponPrefab);
                    Debug.Log("New weapon instance found: " + (newWeaponInstance != null));

                    if (newWeaponInstance != null)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (weaponsManager.GetWeaponAtSlotIndex(i) == newWeaponInstance)
                            {
                                Debug.Log("Switching to weapon slot: " + i);
                                weaponsManager.SwitchToWeaponIndex(i, true);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Weapon was NOT added (already owned or no slot free).");
                }
            }
            else
            {
                Debug.LogWarning("Missing weaponsManager or WeaponPrefab.");
            }

            base.OnPicked(playerController);

            Debug.Log("hi");
            Destroy(gameObject);
            Debug.LogWarning("Destroyed.");
        }



    }
}
