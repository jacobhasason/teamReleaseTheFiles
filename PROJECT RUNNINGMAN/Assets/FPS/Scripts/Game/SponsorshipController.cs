using UnityEngine;

[CreateAssetMenu(fileName = "NewSponsorship", menuName = "Rewards/Sponsorship")]
public class SponsorshipController : ScriptableObject
{
    public string sponsorName;
    public int cost;
    public Sprite icon;

}
