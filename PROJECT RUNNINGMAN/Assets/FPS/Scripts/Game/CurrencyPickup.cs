using UnityEngine;
using Unity.FPS.Gameplay;

public class CurrencyPickup : MonoBehaviour
{
    [Header("Currency Settings")]
    public int amount = 1;                     // Amount of currency this pickup gives
    public CurrencyType type = CurrencyType.AudienceFavor; // Type of currency

    [Header("Audio Settings")]
    public AudioClip pickupSFX;                // Sound to play when picked up
    private AudioSource audioSource;

    void Awake()
    {
        // Ensure an AudioSource exists
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    void Collect()
    {
        // Add currency to the player via CurrencyManager
        CurrencyManager manager = FindObjectOfType<CurrencyManager>();
        if (manager != null)
        {
            manager.AddCurrency(type, amount);
        }
        else
        {
            Debug.LogWarning("CurrencyManager not found in scene!");
        }

        // Play pickup sound
        if (pickupSFX != null)
        {
            audioSource.PlayOneShot(pickupSFX);
        }

        // Destroy pickup after sound finishes or immediately if no SFX
        Destroy(gameObject, pickupSFX != null ? pickupSFX.length : 0f);
    }
}
