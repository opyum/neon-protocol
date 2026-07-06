using UnityEngine;

namespace FirstGame.Combat
{
    /// <summary>Number keys 1..N swap the current weapon (used in the practice range).</summary>
    public class WeaponSwitcher : MonoBehaviour
    {
        public WeaponController weapon;

        void Update()
        {
            if (weapon == null) return;
            int count = Mathf.Min(WeaponCatalog.Weapons.Count, 9);
            for (int i = 0; i < count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    weapon.SwitchWeapon(WeaponCatalog.Weapons[i]);
                    break;
                }
            }
        }
    }
}
