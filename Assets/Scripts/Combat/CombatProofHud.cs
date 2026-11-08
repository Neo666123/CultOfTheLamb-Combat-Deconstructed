using UnityEngine;

namespace MIeMie.Combat
{
    public sealed class CombatProofHud : MonoBehaviour
    {
        private PlayerMovementProof movement;
        private PlayerSwordCombat weaponCombat;

        private void Start()
        {
            movement = FindObjectOfType<PlayerMovementProof>();
            weaponCombat = FindObjectOfType<PlayerSwordCombat>();
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(18f, 18f, 900f, 24f), "WASD 移动 | 空格闪避 | 左键攻击 | Tab / 右侧按钮切换 Sword/Axe/Hammer/Dagger | Scamp HP: 3");
            GUI.Label(new Rect(18f, 42f, 900f, 24f), "Spine Events: Deal Damage -> Hitbox | Can Break -> Queue/Cancel | Finished -> Locomotion");
            if (movement != null)
            {
                GlobalCombatSettings settings = GlobalCombatSettings.Instance;
                GUI.Label(new Rect(18f, 66f, 900f, 24f),
                    string.Format("Lunge inherited speed: {0:F2}   |   current speed: {1:F2}   |   inherit enabled: {2}",
                        movement.LastLungeEntrySpeed, movement.CurrentSpeed, settings.inheritMovementSpeedOnLunge));
            }

            if (weaponCombat != null)
            {
                GUI.Box(new Rect(Screen.width - 330f, 18f, 312f, 108f), string.Empty);
                GUI.Label(new Rect(Screen.width - 316f, 27f, 285f, 24f),
                    "当前武器：" + weaponCombat.CurrentWeaponDisplayName + " / " + weaponCombat.CurrentWeaponName);
                GUI.Label(new Rect(Screen.width - 316f, 51f, 285f, 24f), weaponCombat.CurrentImplementationNote);
                if (GUI.Button(new Rect(Screen.width - 316f, 78f, 285f, 34f), "切换下一把武器（Tab）"))
                    weaponCombat.CycleWeapon(1);
            }
        }
    }
}
