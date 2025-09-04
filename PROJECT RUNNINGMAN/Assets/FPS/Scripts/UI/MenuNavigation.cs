using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS.Game;
using TMPro;
using Unity.FPS.UI;

using UnityEngine.EventSystems;

/// <summary>
/// Handles the shop menu that appears between waves.
/// Player can spend Audience Favor on weapons, sponsorships, or health.
/// Sponsorships unlock Corporate Favor mechanics.
/// </summary>
public class MenuNavigation : MonoBehaviour
{
    // ===============================
    // Static Data (shared across menus)
    // ===============================
    public static int AudienceCurrency;
    public static int CorporateCurrency;
    public static int waveCount;
    public static string sponser;

    [Header("Shop Settings")]
    public int rewardOptions = 3;                  // How many rewards per shop refresh
    public GameObject[] weaponPrefabs;             // Selectable weapons (prefabs with WeaponController)
    public SponsorshipController[] sponsorships;   // Sponsorship assets

    [Header("UI References")]
    public CurrencyManager currencyManager;        // Tracks all currency
    public TextMeshProUGUI AudienceFavorText;      // UI Text for Audience Favor (TMP)
    public TextMeshProUGUI CorporateFavorText;     // UI Text for Corporate Favor (TMP)
    public TextMeshProUGUI ErrorText;              // UI Text for Error Messages (TMP)
    public ButtonHook[] optionButtons;             // Buttons (Selection0, Selection1, Selection2)
    public Sprite healthIcon;                      // Sprite used for Health reward

    [HideInInspector]
    public WaveSpawner waveSpawner;                // Reference to wave manager

    // Cached references to player systems
    PlayerCharacterController playerController;
    Health playerHealth;
    PlayerWeaponsManager playerWeapons;

    // Holds the current shop inventory
    RewardOption[] currentRewards;

    // ===============================
    // Setup
    // ===============================
    void Start()
    {
        if (waveSpawner == null) waveSpawner = FindObjectOfType<WaveSpawner>();
        if (currencyManager == null) currencyManager = FindObjectOfType<CurrencyManager>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerCharacterController>();
            playerHealth = playerObj.GetComponent<Health>();
            playerWeapons = playerObj.GetComponent<PlayerWeaponsManager>();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Sync displayed favor with CurrencyManager
        AudienceCurrency = currencyManager.GetCurrency("AudienceFavor");
        CorporateCurrency = currencyManager.GetCurrency("CorporateFavor");
        AudienceFavorText.text = $"Audience Favor: {AudienceCurrency}";
        CorporateFavorText.text = $"Corporate Favor: {CorporateCurrency}";

        // Apply any sponsorship buffs on load
        UpdateCorporateStatus();

        // Create this round’s shop options
        GenerateRewards();
    }

    // ===============================
    // Player selects a reward option
    // ===============================
    public void OnOptionSelected(int index)
    {
        if (currentRewards == null || index < 0 || index >= currentRewards.Length) return;

        RewardOption chosen = currentRewards[index];

        // Check cost
        if (AudienceCurrency < chosen.Cost)
        {
            Debug.Log("Not enough Audience Favor!");
            ErrorText.text = "Not enough Audience Favor!";
            return;
        }

        // Spend currency
        currencyManager.SpendCurrency(CurrencyType.AudienceFavor, chosen.Cost);
        AudienceCurrency = currencyManager.GetCurrency("AudienceFavor");
        AudienceFavorText.text = $"Audience Favor: {AudienceCurrency}";
        ErrorText.text = "";

        // Apply effect
        switch (chosen.Type)
        {
            case RewardType.Weapon:
                {
                    WeaponController wc = chosen.WeaponPrefab.GetComponent<WeaponController>();
                    if (wc != null && playerWeapons.HasWeapon(wc) == null)
                    {
                        playerWeapons.AddWeapon(wc);
                        Debug.Log($"Purchased weapon: {chosen.DisplayName}");
                    }
                    else
                    {
                        // Already have this weapon → fallback reward
                        playerHealth.Heal(chosen.HealAmount > 0 ? chosen.HealAmount : 50);
                        Debug.Log($"Already had {chosen.DisplayName}, granted health instead.");
                        DisplayMessageEvent weapMessage = Events.DisplayMessageEvent;
                        weapMessage.Message = $"Already had {chosen.DisplayName}, granted health instead.";
                        weapMessage.DelayBeforeDisplay = 2f;
                        EventManager.Broadcast(weapMessage);
                    }
                    break;
                }

            case RewardType.Sponsorship:
                {
                    sponser = chosen.SponsorshipName;
                    Debug.Log($"Purchased sponsorship: {sponser}");
                    // Broadcast new sponser message
                    DisplayMessageEvent sponserMessage = Events.DisplayMessageEvent;
                    sponserMessage.Message = $"Purchased sponsorship: {sponser}";
                    sponserMessage.DelayBeforeDisplay = 2f;
                    EventManager.Broadcast(sponserMessage);
                    DisplayMessageEvent sponserMessage2 = Events.DisplayMessageEvent;
                    sponserMessage2.Message = $"Feel your power grow as you fight...";
                    sponserMessage2.DelayBeforeDisplay = 3f;
                    EventManager.Broadcast(sponserMessage2);
                    currencyManager.hasSponser = true;
                    break;
                }

            case RewardType.Health:
                {
                    playerHealth.Heal(chosen.HealAmount > 0 ? chosen.HealAmount : 50);
                    Debug.Log("Purchased health");
                    break;
                }
        }

        // Tell wave spawner to continue
        waveSpawner?.OnRewardSelected();
    }

    // ===============================
    // Skip purchasing anything
    // ===============================
    public void OnSkip()
    {
        Debug.Log("Skipped reward!");
        waveSpawner?.OnRewardSelected();
    }

    // ===============================
    // Generate random shop inventory
    // ===============================
    void GenerateRewards()
    {
        currentRewards = new RewardOption[rewardOptions];

        for (int i = 0; i < rewardOptions; i++)
        {
            RewardOption option = new RewardOption();
            float roll = Random.value;

            // Weights: 50% weapon, 30% health, 20% sponsorship
            // Weapons
            if (roll < 0.5f && weaponPrefabs != null && weaponPrefabs.Length > 0)
            {
                option.Type = RewardType.Weapon;
                option.WeaponPrefab = weaponPrefabs[Random.Range(0, weaponPrefabs.Length)];

                // Find controller even if it's on a child
                var wc = option.WeaponPrefab.GetComponentInChildren<WeaponController>(true);

                // Prefer the inspector field; fallback to the controller GO name; then prefab name
                if (wc != null)
                {
                    option.DisplayName = !string.IsNullOrWhiteSpace(wc.WeaponName) ? wc.WeaponName : wc.gameObject.name;
                    option.Cost = wc.PurchaseCost;
                    option.Icon = wc.WeaponIcon;
                }
                else
                {
                    // No controller found on this prefab hierarchy
                    option.DisplayName = option.WeaponPrefab.name;
                    option.Cost = 50;         // default fallback cost
                    option.Icon = null;
                }

                // debug to confirm where the name came from
                Debug.Log($"[Shop] Using weapon '{option.DisplayName}' from prefab '{option.WeaponPrefab.name}', wc={(wc ? wc.name : "null")}");

            }
            // Sponsorship
            else if (roll < 0.8f && sponsorships != null && sponsorships.Length > 0 && string.IsNullOrEmpty(sponser))
            {
                option.Type = RewardType.Sponsorship;
                var sc = sponsorships[Random.Range(0, sponsorships.Length)];
                option.SponsorshipName = sc.sponsorName;
                option.DisplayName = sc.sponsorName;
                option.Cost = sc.cost;
                option.Icon = sc.icon;
            }
            // Health
            else
            {
                option.Type = RewardType.Health;
                option.DisplayName = "Health +50";
                option.Cost = 50;
                option.HealAmount = 50;
                option.Icon = healthIcon;
            }

            currentRewards[i] = option;

            // Update the corresponding button UI
            if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
            {
                optionButtons[i].SetData(option.DisplayName, option.Cost, option.Icon);
                Debug.Log("Called SetData!");
            }
                

            Debug.Log($"Reward {i}: {option.DisplayName} ({option.Type}), Cost {option.Cost}");
        }
    }

    // ===============================
    // Sponsorship buffs (applied from Corporate Favor)
    // ===============================
    void UpdateCorporateStatus()
    {
        switch (sponser)
        {
            case "Colossus Mining Company":
                {
                    // +10 damage per 50 corporate favor -> write to AdditionalDamage on each held weapon
                    float bonus = (currencyManager.CorporateFavor / 50) * 10f;

                    foreach (var w in playerWeapons.m_WeaponSlots)
                    {
                        if (!w) continue;

                        // ranged
                        var wc = w.GetComponent<WeaponController>();
                        if (wc) wc.AdditionalDamage = bonus;

                        // melee
                        var mc = w.GetComponent<MeleeWeaponController>();
                        if (mc) mc.AdditionalDamage = bonus;
                    }
                    break;
                }

            case "Quantum Kinetics Incorporated":
                {
                    // +0.5 speed & jump per 10 corporate favor until 50 jump
                    if(playerController.JumpForce < 50)
                    {
                        float steps = (currencyManager.CorporateFavor / 10) * 0.5f;
                        playerController.MaxSpeedOnGround += steps;
                        playerController.JumpForce += steps;
                        playerController.MaxSpeedInAir += steps;
                    }
                    
                    // Gradually reduce fall damage
                    if (currencyManager.CorporateFavor >= 50 && playerController.FallDamageAtMaxSpeed > 0);
                    {
                        playerController.FallDamageAtMinSpeed -= (currencyManager.CorporateFavor / 10);
                        playerController.FallDamageAtMaxSpeed -= (currencyManager.CorporateFavor / 10);
                    }
                    
                    break;
                }

            case "Heartland Harvesters Conglomerate":
                {
                    // +10 max health per 50 corporate favor
                    float hpBonus = (currencyManager.CorporateFavor / 50) * 10f;
                    playerHealth.MaxHealth += hpBonus;

                    // After 300 -> auto heal each wave
                    if (currencyManager.CorporateFavor >= 300)
                    {
                        playerHealth.Heal(playerHealth.MaxHealth);
                    }
                    break;
                }
        }
    }
}

// ===============================
// Reward System Data Structures
// ===============================
public enum RewardType
{
    Weapon,
    Sponsorship,
    Health
}

[System.Serializable]
public class RewardOption
{
    public RewardType Type;

    [Header("Common Fields")]
    public string DisplayName;
    public int Cost;
    public Sprite Icon;

    [Header("Weapon Data")]
    public GameObject WeaponPrefab;

    [Header("Sponsorship Data")]
    public string SponsorshipName;

    [Header("Health Data")]
    public int HealAmount;
}
