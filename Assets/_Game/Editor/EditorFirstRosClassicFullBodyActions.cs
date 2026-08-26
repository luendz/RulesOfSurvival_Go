using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa la estructura oficial del layer FullBody_Actions del
    /// Animator ROS Classic sin sobrescribir motions ni ajustes manuales.
    ///
    /// Este paso crea únicamente la arquitectura visible/editable. El runtime
    /// de Vault, AirDrop, Swimming, Vehicle, Knocked, Gestures y Death se conecta
    /// en pasos posteriores para evitar condiciones inventadas o transiciones
    /// que puedan competir entre sí antes de existir una fuente de estado real.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicFullBodyActions
    {
        private const string FreeFallClipPath =
            "Assets/_Game/Animations/Character Animator/03. Parachute/Ch28_nonPBR@Falling Parachutec.fbx";

        private const string DriverIdleClipPath =
            "Assets/_Game/Animations/Character Animator/11. Vehicle/Driver/Ch28_nonPBR@Driver Idle.fbx";

        static EditorFirstRosClassicFullBodyActions()
        {
            EditorApplication.delayCall += ScheduleMaterialization;
        }

        private static void ScheduleMaterialization()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/15 - Construir FullBody Actions")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                EditorFirstRosClassicAnimatorBuilder.ControllerPath
            );

            if (controller == null)
            {
                EditorFirstRosClassicAnimatorBuilder.CreateIfMissing();
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );
            }

            if (controller == null)
                return;

            bool changed = false;

            changed |= EnsureParameter(
                controller,
                "IsVaulting",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "IsSwimming",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "IsUnderwater",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "IsParachuting",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "IsFreeFalling",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "IsKnocked",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "FullBodyAction",
                AnimatorControllerParameterType.Int
            );

            AnimatorControllerLayer layer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.FullBodyActionsLayer
            );

            if (layer == null || layer.stateMachine == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se encontró el layer FullBody_Actions. " +
                    "Ejecuta primero '01 - Crear arquitectura base'."
                );
                return;
            }

            AnimatorStateMachine root = layer.stateMachine;

            AnimatorState empty = FindState(root, "Empty");
            if (empty == null)
            {
                empty = root.AddState("Empty", new Vector3(260f, 40f, 0f));
                empty.writeDefaultValues = false;
                changed = true;
            }

            if (root.defaultState == null)
            {
                root.defaultState = empty;
                changed = true;
            }

            AnimatorStateMachine vault = EnsureStateMachine(
                root,
                "Vault",
                new Vector3(520f, -220f, 0f),
                ref changed
            );
            EnsureState(vault, "Vault Low", new Vector3(240f, 20f, 0f), null, ref changed);
            EnsureState(vault, "Vault Window", new Vector3(500f, 20f, 0f), null, ref changed);
            EnsureState(vault, "Vault High", new Vector3(760f, 20f, 0f), null, ref changed);

            AnimatorStateMachine airDrop = EnsureStateMachine(
                root,
                "AirDrop",
                new Vector3(800f, -220f, 0f),
                ref changed
            );
            EnsureState(airDrop, "Exit Aircraft", new Vector3(180f, 20f, 0f), null, ref changed);
            EnsureState(airDrop, "FreeFall Enter", new Vector3(420f, 20f, 0f), null, ref changed);
            EnsureState(
                airDrop,
                "FreeFall",
                new Vector3(660f, 20f, 0f),
                FreeFallClipPath,
                ref changed
            );
            EnsureState(airDrop, "Parachute Deploy", new Vector3(900f, 20f, 0f), null, ref changed);
            EnsureState(airDrop, "Parachute Glide", new Vector3(1140f, 20f, 0f), null, ref changed);
            EnsureState(airDrop, "Parachute Land", new Vector3(1380f, 20f, 0f), null, ref changed);

            AnimatorStateMachine swimming = EnsureStateMachine(
                root,
                "Swimming",
                new Vector3(1080f, -220f, 0f),
                ref changed
            );
            EnsureState(swimming, "Swim Idle", new Vector3(220f, 20f, 0f), null, ref changed);
            EnsureState(swimming, "Swim Forward", new Vector3(480f, -80f, 0f), null, ref changed);
            EnsureState(swimming, "Swim Left", new Vector3(480f, 20f, 0f), null, ref changed);
            EnsureState(swimming, "Swim Right", new Vector3(480f, 120f, 0f), null, ref changed);
            EnsureState(swimming, "Underwater Swim", new Vector3(760f, 20f, 0f), null, ref changed);

            AnimatorStateMachine vehicle = EnsureStateMachine(
                root,
                "Vehicle",
                new Vector3(1360f, -220f, 0f),
                ref changed
            );
            EnsureState(vehicle, "Enter", new Vector3(220f, 20f, 0f), null, ref changed);
            EnsureState(
                vehicle,
                "Driver",
                new Vector3(500f, -50f, 0f),
                DriverIdleClipPath,
                ref changed
            );
            EnsureState(vehicle, "Passenger", new Vector3(500f, 100f, 0f), null, ref changed);
            EnsureState(vehicle, "Exit", new Vector3(780f, 20f, 0f), null, ref changed);

            AnimatorStateMachine knocked = EnsureStateMachine(
                root,
                "Knocked",
                new Vector3(520f, 260f, 0f),
                ref changed
            );
            EnsureState(knocked, "KnockDown", new Vector3(220f, 20f, 0f), null, ref changed);
            EnsureState(knocked, "Knocked Idle", new Vector3(480f, -60f, 0f), null, ref changed);
            EnsureState(knocked, "Knocked Crawl", new Vector3(480f, 100f, 0f), null, ref changed);
            EnsureState(knocked, "Revived", new Vector3(760f, 20f, 0f), null, ref changed);

            AnimatorStateMachine gestures = EnsureStateMachine(
                root,
                "Gestures",
                new Vector3(800f, 260f, 0f),
                ref changed
            );
            EnsureState(gestures, "Gesture 01", new Vector3(220f, -90f, 0f), null, ref changed);
            EnsureState(gestures, "Gesture 02", new Vector3(220f, 20f, 0f), null, ref changed);
            EnsureState(gestures, "Gesture 03", new Vector3(500f, -90f, 0f), null, ref changed);
            EnsureState(gestures, "Gesture 04", new Vector3(500f, 20f, 0f), null, ref changed);

            AnimatorStateMachine death = EnsureStateMachine(
                root,
                "Death",
                new Vector3(1080f, 260f, 0f),
                ref changed
            );
            EnsureState(death, "Death Forward", new Vector3(220f, -100f, 0f), null, ref changed);
            EnsureState(death, "Death Backward", new Vector3(220f, 20f, 0f), null, ref changed);
            EnsureState(death, "Death Left", new Vector3(500f, -100f, 0f), null, ref changed);
            EnsureState(death, "Death Right", new Vector3(500f, 20f, 0f), null, ref changed);
            EnsureState(death, "Death Variant 01", new Vector3(780f, -40f, 0f), null, ref changed);

            if (!changed)
            {
                Debug.Log(
                    "[ROS Classic Animator] FullBody_Actions ya está estructurado. " +
                    "No se modificaron motions ni ajustes manuales."
                );
                return;
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(vault);
            EditorUtility.SetDirty(airDrop);
            EditorUtility.SetDirty(swimming);
            EditorUtility.SetDirty(vehicle);
            EditorUtility.SetDirty(knocked);
            EditorUtility.SetDirty(gestures);
            EditorUtility.SetDirty(death);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Paso 15 completado: FullBody_Actions estructurado con " +
                "Vault, AirDrop, Swimming, Vehicle, Knocked, Gestures y Death. " +
                "No se añadieron transiciones de runtime todavía y no se sobrescribieron motions existentes."
            );
        }

        private static AnimatorControllerLayer FindLayer(
            AnimatorController controller,
            string layerName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName)
                    return layers[i];
            }

            return null;
        }

        private static AnimatorStateMachine EnsureStateMachine(
            AnimatorStateMachine parent,
            string name,
            Vector3 position,
            ref bool changed)
        {
            ChildAnimatorStateMachine[] children = parent.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine existing = children[i].stateMachine;
                if (existing != null && existing.name == name)
                    return existing;
            }

            AnimatorStateMachine created = parent.AddStateMachine(name, position);
            changed = true;
            return created;
        }

        private static AnimatorState EnsureState(
            AnimatorStateMachine machine,
            string stateName,
            Vector3 position,
            string clipPath,
            ref bool changed)
        {
            AnimatorState existing = FindState(machine, stateName);
            if (existing != null)
                return existing;

            AnimatorState state = machine.AddState(stateName, position);
            state.writeDefaultValues = false;

            if (!string.IsNullOrWhiteSpace(clipPath))
            {
                AnimationClip clip = LoadFirstAnimationClip(clipPath);
                if (clip != null)
                {
                    state.motion = clip;
                }
                else
                {
                    Debug.LogWarning(
                        "[ROS Classic Animator] Se creó " + machine.name + "/" + stateName +
                        " pero no se encontró el clip conocido: " + clipPath
                    );
                }
            }

            changed = true;
            return state;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string stateName)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == stateName)
                    return state;
            }

            return null;
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
                        "[ROS Classic Animator] El parámetro '" + name +
                        "' ya existe con un tipo distinto. No se reemplazó para evitar romper el Animator."
                    );
                }

                return false;
            }

            controller.AddParameter(name, type);
            return true;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__"))
                    continue;

                return clip;
            }

            return null;
        }
    }
}
