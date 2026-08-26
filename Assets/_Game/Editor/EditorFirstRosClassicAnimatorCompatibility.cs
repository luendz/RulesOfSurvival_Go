using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad temporal entre el runtime legacy de armas y el Animator ROS Classic.
    ///
    /// WeaponEquipmentController todavia puede escribir HasRifle/WeaponSwitch. El Animator
    /// ROS Classic ya selecciona la familia mediante WeaponType, pero mientras terminamos
    /// la migracion estos parametros se mantienen para evitar warnings de Animator.
    ///
    /// No modifica estados, transiciones ni motions asignados manualmente.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicAnimatorCompatibility
    {
        private const string ClassicWeaponLayer = "UpperBody_Weapon";
        private const string WeaponTypeParameter = "WeaponType";
        private const string LegacyHasRifleParameter = "HasRifle";
        private const string LegacyWeaponSwitchParameter = "WeaponSwitch";

        static EditorFirstRosClassicAnimatorCompatibility()
        {
            EditorApplication.delayCall += EnsureCompatibility;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/97 - Asegurar compatibilidad de parametros")]
        public static void EnsureCompatibility()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
            bool anyChanged = false;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AnimatorController controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

                if (!IsRosClassicController(controller))
                    continue;

                bool changed = false;
                changed |= EnsureParameter(
                    controller,
                    LegacyHasRifleParameter,
                    AnimatorControllerParameterType.Bool
                );
                changed |= EnsureParameter(
                    controller,
                    LegacyWeaponSwitchParameter,
                    AnimatorControllerParameterType.Trigger
                );

                if (!changed)
                    continue;

                EditorUtility.SetDirty(controller);
                anyChanged = true;

                Debug.Log(
                    "[ROS Classic Animator] Parametros de compatibilidad verificados en: " +
                    path + ". HasRifle solo se conserva para runtime legacy; WeaponType sigue " +
                    "siendo el selector real de familia de arma."
                );
            }

            if (!anyChanged)
                return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool IsRosClassicController(AnimatorController controller)
        {
            if (controller == null)
                return false;

            bool hasWeaponType = false;
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == WeaponTypeParameter &&
                    parameters[i].type == AnimatorControllerParameterType.Int)
                {
                    hasWeaponType = true;
                    break;
                }
            }

            if (!hasWeaponType)
                return false;

            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == ClassicWeaponLayer)
                    return true;
            }

            return false;
        }

        private static bool EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name != name)
                    continue;

                if (parameters[i].type != type)
                {
                    Debug.LogWarning(
                        "[ROS Classic Animator] El parametro '" + name +
                        "' ya existe con un tipo distinto en " + controller.name +
                        ". No se modifica automaticamente."
                    );
                }

                return false;
            }

            controller.AddParameter(name, type);
            return true;
        }
    }
}
