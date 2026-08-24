using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Capa de estabilidad visual de los slots de armas.
    /// El loot de muertos ya no se controla aquí para evitar múltiples writers
    /// sobre Canvas/NearbyLoot.
    /// </summary>
    [DefaultExecutionOrder(2200)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDStabilityFix : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";

        private static readonly Color SlotNormal =
            new Color(0.08f, 0.095f, 0.105f, 0.88f);

        private static readonly Color SlotSelected =
            new Color(0.16f, 0.17f, 0.17f, 0.94f);

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.06f, 1f);

        private PlayerInputReader _localInput;
        private WeaponEquipmentController _weapons;
        private float _nextResolveTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDStabilityFix>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_StabilityFix")
                .AddComponent<RulesOfSurvivalHUDStabilityFix>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.20f;
                ResolveLocalPlayer();
            }

            StabilizeWeaponSlots();
        }

        private void ResolveLocalPlayer()
        {
            if (!IsValidLocalInput(_localInput))
            {
                _localInput = FindLocalPlayerInput();
                _weapons = null;
            }

            if (_localInput != null && _weapons == null)
            {
                _weapons = _localInput.GetComponent<WeaponEquipmentController>();
            }
        }

        private void StabilizeWeaponSlots()
        {
            if (_weapons == null)
            {
                return;
            }

            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform status = hud.transform.Find("Canvas/PlayerStatusFidelity");
            if (status == null)
            {
                return;
            }

            for (int slot = 1; slot <= 4; slot++)
            {
                Transform root = status.Find($"WeaponSlots/Slot_{slot}");
                if (root == null)
                {
                    continue;
                }

                bool selected =
                    slot <= 3 &&
                    _weapons.EquippedSlot == slot &&
                    _weapons.GetWeaponForSlot(slot) != null;

                Image background = root.GetComponent<Image>();
                if (background != null)
                {
                    background.color = selected ? SlotSelected : SlotNormal;
                }

                Image selection = root.Find("Selection")?.GetComponent<Image>();
                if (selection != null)
                {
                    selection.color = selected ? Yellow : Color.clear;
                }

                Text number = root.Find("Number")?.GetComponent<Text>();
                if (number != null)
                {
                    number.color = selected
                        ? Yellow
                        : new Color(1f, 1f, 1f, 0.82f);
                }
            }
        }

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs =
                Resources.FindObjectsOfTypeAll<PlayerInputReader>();

            Scene activeScene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) ||
                    candidate.gameObject.scene != activeScene)
                {
                    continue;
                }

                if (candidate.gameObject.name == "Player_Prototype" ||
                    candidate.gameObject.name.StartsWith("Player_"))
                {
                    return candidate;
                }

                if (fallback == null &&
                    !candidate.gameObject.name.StartsWith("Bot_"))
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private static bool IsValidLocalInput(PlayerInputReader input)
        {
            return input != null &&
                   input.gameObject.scene.IsValid() &&
                   !input.UsesExternalControl;
        }
    }
}
