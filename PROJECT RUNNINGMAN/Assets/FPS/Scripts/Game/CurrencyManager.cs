using UnityEngine;
using UnityEngine.UI;

// The two types of currency in the game.
// AudienceFavor = default currency earned from kills
// CorporateFavor = unlocked later when sponsorship is purchased
namespace Unity.FPS.Gameplay { 

    public enum CurrencyType
    {
    AudienceFavor,
    CorporateFavor
    }

    public class CurrencyManager : MonoBehaviour
    {
        // Current amounts stored in memory
        public int AudienceFavor { get; private set; }
        public int CorporateFavor { get; private set; }
        public bool hasSponser = false;

        void Start()
        {
            // How much Audience Favor the player starts with
            AddCurrency(CurrencyType.AudienceFavor, 500); 
        }

        // Add currency of a given type
        public void AddCurrency(CurrencyType type, int amount)
        {
            switch (type)
            {
                case CurrencyType.AudienceFavor:
                    AudienceFavor += amount;
                    break;

                case CurrencyType.CorporateFavor:
                    CorporateFavor += amount;
                    break;
            }

            // Debug to confirm it's working (remove later for production)
            Debug.Log($"{amount} {type} added. Current totals → Audience: {AudienceFavor}, Corporate: {CorporateFavor}");
        }

        // Spend currency if the player has enough
        public bool SpendCurrency(CurrencyType type, int amount)
        {
            switch (type)
            {
                case CurrencyType.AudienceFavor:
                    if (AudienceFavor >= amount)
                    {
                        AudienceFavor -= amount;
                        return true; // transaction success
                    }
                    break;

                case CurrencyType.CorporateFavor:
                    if (CorporateFavor >= amount)
                    {
                        CorporateFavor -= amount;
                        return true;
                    }
                    break;
            }

            // Transaction failed (not enough currency)
            return false;
        }

        // Query total of a given type
        public int GetCurrency(string type)
        {
            if (type == "AudienceFavor")
            {
                return AudienceFavor;
            }
            else if (type == "CorporateFavor")
            {
                return CorporateFavor;
            }
            else
            {
                return 0;
            }
        }
    }
}