using UnityEngine;

// The two types of currency in the game.
// AudienceFavor = default currency earned from kills
// CorporateFavor = unlocked later when sponsorship is purchased
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
    public int GetCurrency(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.AudienceFavor => AudienceFavor,
            CurrencyType.CorporateFavor => CorporateFavor,
            _ => 0
        };
    }

   /* public void UnlockCorporateSponsorship()
    {
        if (!currencies.ContainsKey(CurrencyType.CorporateFavor))
        {
            currencies[CurrencyType.CorporateFavor] = 0;
            Debug.Log("Corporate Sponsorship unlocked! Now you can earn Corporate Favor.");
        }
    }
   */
}