using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS.Game;

using UnityEngine.EventSystems;

public class MenuNavigation : MonoBehaviour
{
    // Data to be passed in
    public static int AudienceCurrency;
    public static int CorporateCurrency;
    public static int waveCount;

    [Header("Weapon Rewards")]
    public GameObject[] weaponPrefabs; // Assign weapon prefabs here in Inspector

    [HideInInspector]
    public WaveSpawner waveSpawner;

    PlayerCharacterController playerController;
    Health playerHealth;
    PlayerWeaponsManager playerWeapons;

    void Start()
    {
        if (waveSpawner == null)
            waveSpawner = FindObjectOfType<WaveSpawner>();

        if (waveSpawner == null)
            Debug.Log("No wave spawner found!");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerCharacterController>();
            playerHealth = playerObj.GetComponent<Health>();
            playerWeapons = playerObj.GetComponent<PlayerWeaponsManager>();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Menu started");
    }

    public void OnOptionSelected(int index)
    {
        Debug.Log("Option " + index + " selected!");
        GiveRandomReward();
    }

    public void OnSkip()
    {
        Debug.Log("Skipped reward!");
        waveSpawner?.OnRewardSelected(); // call back to WaveSpawner
    }

    void GiveRandomReward()
    {
        if (playerHealth == null || playerWeapons == null)
        {
            Debug.LogWarning("Player components not found, can’t give reward!");
            return;
        }

        if (weaponPrefabs.Length > 0)
        {
            // Pick a random weapon prefab
            int randIndex = Random.Range(0, weaponPrefabs.Length);
            GameObject weaponPrefab = weaponPrefabs[randIndex];

            WeaponController weapon = weaponPrefab.GetComponent<WeaponController>();
            if (weapon != null)
            {
                // Check if player already owns this weapon using HasWeapon()
                if (playerWeapons.HasWeapon(weapon) == null)
                {
                    playerWeapons.AddWeapon(weapon);
                    Debug.Log("Granted weapon: " + weaponPrefab.name);

                    // Display pickup message
                    DisplayMessageEvent itemMessage = Events.DisplayMessageEvent;
                    itemMessage.Message = $"{weaponPrefab.name} Collected!";
                    itemMessage.DelayBeforeDisplay = 2f;
                    EventManager.Broadcast(itemMessage);
                }
                else
                {
                    // Player already has weapon, grant health instead
                    playerHealth.Heal(50f);
                    Debug.Log("Player already has " + weaponPrefab.name + ", granted +50 Health instead.");

                    DisplayMessageEvent itemMessage = Events.DisplayMessageEvent;
                    itemMessage.Message = "+50 Health Granted!";
                    itemMessage.DelayBeforeDisplay = 2f;
                    EventManager.Broadcast(itemMessage);
                }
            }
            else
            {
                Debug.LogWarning("Weapon prefab " + weaponPrefab.name + " has no WeaponController!");
            }
        }
        else
        {
            // No weapon prefabs assigned, just give health
            playerHealth.Heal(50f);
            Debug.Log("No weapon prefabs - Granted +50 Health");

            DisplayMessageEvent itemMessage = Events.DisplayMessageEvent;
            itemMessage.Message = "+50 Health Granted!";
            itemMessage.DelayBeforeDisplay = 2f;
            EventManager.Broadcast(itemMessage);
        }

        // Notify wave spawner reward was handled
        waveSpawner?.OnRewardSelected();
    }


}


