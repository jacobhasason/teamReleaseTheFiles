using TMPro;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    [RequireComponent(typeof(FillBarColorChange))]
    public class AmmoCounter : MonoBehaviour
    {
        [Tooltip("CanvasGroup to fade the ammo UI")]
        public CanvasGroup CanvasGroup;

        [Tooltip("Image for the weapon icon")] public Image WeaponImage;

        [Tooltip("Image component for the background")]
        public Image AmmoBackgroundImage;

        [Tooltip("Image component to display fill ratio")]
        public Image AmmoFillImage;

        [Tooltip("Text for Weapon index")] 
        public TextMeshProUGUI WeaponIndexText;

        [Tooltip("Text for Bullet Counter")] 
        public TextMeshProUGUI BulletCounter;

        [Tooltip("Reload Text for Weapons with physical bullets")]
        public RectTransform Reload;

        [Header("Selection")] [Range(0, 1)] [Tooltip("Opacity when weapon not selected")]
        public float UnselectedOpacity = 0.5f;

        [Tooltip("Scale when weapon not selected")]
        public Vector3 UnselectedScale = Vector3.one * 0.8f;

        [Tooltip("Root for the control keys")] public GameObject ControlKeysRoot;

        [Header("Feedback")] [Tooltip("Component to animate the color when empty or full")]
        public FillBarColorChange FillBarColorChange;

        [Tooltip("Sharpness for the fill ratio movements")]
        public float AmmoFillMovementSharpness = 20f;

        [Tooltip("Determines if ammo count should be ignored - (Melee weapon)")]
        bool _ignoreAmmo;


        public int WeaponCounterIndex { get; set; }

        PlayerWeaponsManager m_PlayerWeaponsManager;
        WeaponController m_Weapon;

        void Awake()
        {
            EventManager.AddListener<AmmoPickupEvent>(OnAmmoPickup);
        }

        void OnAmmoPickup(AmmoPickupEvent evt)
        {
            if (evt.Weapon == m_Weapon)
            {
                BulletCounter.text = m_Weapon.GetCarriedPhysicalBullets().ToString();
            }
        }

        public void Initialize(WeaponController weapon, int weaponIndex)
        {
            m_Weapon = weapon;
            _ignoreAmmo = m_Weapon != null && m_Weapon.IsMelee;

            WeaponCounterIndex = weaponIndex;
            WeaponImage.sprite = weapon.WeaponIcon;

            // Hide bullets for melee
            if (!weapon.HasPhysicalBullets || _ignoreAmmo)
                BulletCounter.transform.parent.gameObject.SetActive(false);
            else
                BulletCounter.text = weapon.GetCarriedPhysicalBullets().ToString();

            Reload.gameObject.SetActive(false);

            m_PlayerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, AmmoCounter>(m_PlayerWeaponsManager, this);

            WeaponIndexText.text = (WeaponCounterIndex + 1).ToString();

            // For melee, force safe thresholds (full bar, no “low” state)
            if (_ignoreAmmo)
                FillBarColorChange.Initialize(1f, 0f); // zero “needed to shoot” so it never goes red
            else
                FillBarColorChange.Initialize(1f, m_Weapon.GetAmmoNeededToShoot());

        }

        void Update()
        {
            float currentFillRatio = _ignoreAmmo ? 1f : m_Weapon.CurrentAmmoRatio;

            // keep the bar full & skip red logic for melee
            AmmoFillImage.fillAmount = Mathf.Lerp(AmmoFillImage.fillAmount, currentFillRatio,
                Time.deltaTime * AmmoFillMovementSharpness);

            if (!_ignoreAmmo && m_Weapon.HasPhysicalBullets)
                BulletCounter.text = m_Weapon.GetCarriedPhysicalBullets().ToString();

            bool isActiveWeapon = m_Weapon == m_PlayerWeaponsManager.GetActiveWeapon();

            CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, isActiveWeapon ? 1f : UnselectedOpacity, Time.deltaTime * 10);
            transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? Vector3.one : UnselectedScale, Time.deltaTime * 10);
            ControlKeysRoot.SetActive(!isActiveWeapon);

            // Skip color-change logic for melee so it never goes red
            if (_ignoreAmmo)
                FillBarColorChange.UpdateVisual(1f);
            else
                FillBarColorChange.UpdateVisual(currentFillRatio);

            // Only show “Reload” for guns
            Reload.gameObject.SetActive(!_ignoreAmmo &&
                m_Weapon.HasPhysicalBullets &&
                m_Weapon.GetCarriedPhysicalBullets() > 0 &&
                m_Weapon.GetCurrentAmmo() == 0 &&
                m_Weapon.IsWeaponActive);

        }

        void Destroy()
        {
            EventManager.RemoveListener<AmmoPickupEvent>(OnAmmoPickup);
        }
    }
}