using UnityEngine;
using Unity.FPS.Game;

[CreateAssetMenu(fileName = "WeaponPickupData", menuName = "Weapon Pickup Data", order = 1)]
public class WeaponPickupData : ScriptableObject
{
    public GameObject pickupPrefab;
}
