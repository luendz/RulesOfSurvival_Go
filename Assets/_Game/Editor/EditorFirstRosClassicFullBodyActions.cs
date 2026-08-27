using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa la estructura oficial del layer FullBody_Actions del
    /// Animator ROS Classic sin sobrescribir motions ni ajustes manuales.
    /// Conecta únicamente fuentes runtime que ya existen: AirDrop y gestos.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicFullBodyActions
    {
        private const int VehicleFullBodyAction = 4;
        private const string FreeFallClipPath =
            "Assets/_Game/Animations/Character Animator/03. Parachute/Ch28_nonPBR@Falling Parachutec.fbx";

        private const string DriverIdleClipPath =
            "Assets/_Game/Animations/Character Animator/11. Vehicle/Driver/Ch28_nonPBR@Driver Idle.fbx";

        private static readonly string[] UpperBodyGestureNames =
        {
            "Gesture_Salute",
            "Gesture_Talking_On_Phone",
            "Gesture_Waving_Gesture"
        };

        private static readonly string[] UpperBodyGesturePaths =
        {
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Salute.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Talking On Phone.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Waving Gesture.fbx"
        };

        private static readonly string[] FullBodyGestureNames =
        {
            "Gesture_Dancing",
            "Gesture_Fishing_Cast",
            "Gesture_Hip_Hop_Dancing",
            "Gesture_Joyful_Jump",
            "Gesture_Opening",
            "Gesture_Rumba_Dancing"
        };

        private static readonly string[] FullBodyGesturePaths =
        {
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Dancing.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Fishing Cast.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Hip Hop Dancing.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Joyful Jump.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Opening.fbx",
            "Assets/_Game/Animations/Character Animator/08. Emotes/Ch28_nonPBR@Rumba Dancing.fbx"
        };

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
            AnimatorState freeFall = EnsureState(
                airDrop,
                "FreeFall",
                new Vector3(660f, 20f, 0f),
                FreeFallClipPath,
                ref changed
            );
            EnsureState(airDrop, "Parachute Deploy", new Vector3(900f, 20f, 0f), null, ref changed);
            AnimatorState parachuteGlide = EnsureState(
                airDrop,
                "Parachute Glide",
                new Vector3(1140f, 20f, 0f),
                null,
                ref changed
            );
            EnsureState(airDrop, "Parachute Land", new Vector3(1380f, 20f, 0f), null, ref changed);

            if (freeFall != null &&
                (airDrop.defaultState == null ||
                 (airDrop.defaultState.name == "Exit Aircraft" &&
                  airDrop.defaultState.motion == null)))
            {
                airDrop.defaultState = freeFall;
                changed = true;
            }

            EnsureAirDropTransitions(
                root,
                empty,
                airDrop,
                freeFall,
                parachuteGlide,
                ref changed
            );

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
            AnimatorState driver = EnsureState(
                vehicle,
                "Driver",
                new Vector3(500f, -50f, 0f),
                DriverIdleClipPath,
                ref changed
            );
            EnsureState(vehicle, "Passenger", new Vector3(500f, 100f, 0f), null, ref changed);
            EnsureState(vehicle, "Exit", new Vector3(780f, 20f, 0f), null, ref changed);

            if (driver != null && driver.motion != null)
            {
                if (vehicle.defaultState == null ||
                    (vehicle.defaultState.name == "Enter" &&
                     vehicle.defaultState.motion == null))
                {
                    vehicle.defaultState = driver;
                    changed = true;
                }

                EnsureFullBodyActionTransitions(
                    root,
                    empty,
                    vehicle,
                    VehicleFullBodyAction,
                    ref changed
                );
            }

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
            EnsureGestureStates(
                gestures,
                FullBodyGestureNames,
                FullBodyGesturePaths,
                ref changed
            );
            EnsureStateMachineReturn(root, gestures, empty, ref changed);

            AnimatorControllerLayer upperBodyLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyActionsLayer
            );
            AnimatorStateMachine upperBodyGestures = null;
            if (upperBodyLayer != null && upperBodyLayer.stateMachine != null)
            {
                AnimatorStateMachine upperRoot = upperBodyLayer.stateMachine;
                AnimatorState upperEmpty = FindState(upperRoot, "Empty");
                if (upperEmpty != null)
                {
                    upperBodyGestures = EnsureStateMachine(
                        upperRoot,
                        "Gestures",
                        new Vector3(1680f, -280f, 0f),
                        ref changed
                    );
                    EnsureGestureStates(
                        upperBodyGestures,
                        UpperBodyGestureNames,
                        UpperBodyGesturePaths,
                        ref changed
                    );
                    EnsureStateMachineReturn(
                        upperRoot,
                        upperBodyGestures,
                        upperEmpty,
                        ref changed
                    );
                    EditorUtility.SetDirty(upperRoot);
                }
            }

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
            if (upperBodyGestures != null)
                EditorUtility.SetDirty(upperBodyGestures);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Paso 15 completado: FullBody_Actions estructurado con " +
                "Vault, AirDrop, Swimming, Vehicle, Knocked, Gestures y Death. " +
                "AirDrop y los nueve gestos quedaron conectados sin sobrescribir motions existentes."
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
            {
                if (existing.motion == null && !string.IsNullOrWhiteSpace(clipPath))
                {
                    AnimationClip existingClip = LoadFirstAnimationClip(clipPath);
                    if (existingClip != null)
                    {
                        existing.motion = existingClip;
                        EditorUtility.SetDirty(existing);
                        changed = true;
                    }
                }

                return existing;
            }

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

        private static void EnsureGestureStates(
            AnimatorStateMachine machine,
            string[] stateNames,
            string[] clipPaths,
            ref bool changed)
        {
            for (int i = 0; i < stateNames.Length; i++)
            {
                float x = 220f + (i % 2) * 320f;
                float y = 160f + (i / 2) * 110f;
                AnimatorState state = EnsureState(
                    machine,
                    stateNames[i],
                    new Vector3(x, y, 0f),
                    clipPaths[i],
                    ref changed
                );

                if (state == null)
                    continue;

                if (state.tag != "Gesture")
                {
                    state.tag = "Gesture";
                    EditorUtility.SetDirty(state);
                    changed = true;
                }

                bool hasExit = false;
                AnimatorStateTransition[] transitions = state.transitions;
                for (int t = 0; t < transitions.Length; t++)
                {
                    if (transitions[t] != null && transitions[t].isExit)
                    {
                        hasExit = true;
                        break;
                    }
                }

                if (hasExit)
                    continue;

                AnimatorStateTransition exit = state.AddExitTransition();
                exit.hasExitTime = true;
                exit.exitTime = 0.98f;
                exit.hasFixedDuration = true;
                exit.duration = 0.12f;
                EditorUtility.SetDirty(state);
                changed = true;
            }
        }

        private static void EnsureStateMachineReturn(
            AnimatorStateMachine root,
            AnimatorStateMachine source,
            AnimatorState destination,
            ref bool changed)
        {
            AnimatorTransition[] transitions =
                root.GetStateMachineTransitions(source);
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null &&
                    transitions[i].destinationState == destination)
                {
                    return;
                }
            }

            root.AddStateMachineTransition(source, destination);
            changed = true;
        }

        private static void EnsureAirDropTransitions(
            AnimatorStateMachine root,
            AnimatorState empty,
            AnimatorStateMachine airDrop,
            AnimatorState freeFall,
            AnimatorState parachuteGlide,
            ref bool changed)
        {
            EnsureStateToMachineBoolTransition(
                empty,
                airDrop,
                "IsFreeFalling",
                ref changed
            );
            EnsureStateToMachineBoolTransition(
                empty,
                airDrop,
                "IsParachuting",
                ref changed
            );

            AnimatorTransition[] exits = root.GetStateMachineTransitions(airDrop);
            bool hasReturn = false;
            for (int i = 0; i < exits.Length; i++)
            {
                AnimatorTransition transition = exits[i];
                if (transition == null || transition.destinationState != empty)
                    continue;

                bool freeFallFalse = HasCondition(
                    transition.conditions,
                    "IsFreeFalling",
                    AnimatorConditionMode.IfNot
                );
                bool parachutingFalse = HasCondition(
                    transition.conditions,
                    "IsParachuting",
                    AnimatorConditionMode.IfNot
                );
                if (freeFallFalse && parachutingFalse)
                {
                    hasReturn = true;
                    break;
                }
            }

            if (!hasReturn)
            {
                AnimatorTransition transition =
                    root.AddStateMachineTransition(airDrop, empty);
                transition.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsFreeFalling"
                );
                transition.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsParachuting"
                );
                changed = true;
            }

            if (freeFall == null ||
                parachuteGlide == null ||
                parachuteGlide.motion == null)
            {
                return;
            }

            AnimatorStateTransition[] stateTransitions = freeFall.transitions;
            for (int i = 0; i < stateTransitions.Length; i++)
            {
                AnimatorStateTransition transition = stateTransitions[i];
                if (transition != null &&
                    transition.destinationState == parachuteGlide &&
                    HasCondition(
                        transition.conditions,
                        "IsParachuting",
                        AnimatorConditionMode.If))
                {
                    return;
                }
            }

            AnimatorStateTransition toGlide = freeFall.AddTransition(parachuteGlide);
            toGlide.hasExitTime = false;
            toGlide.hasFixedDuration = true;
            toGlide.duration = 0.08f;
            toGlide.AddCondition(AnimatorConditionMode.If, 0f, "IsParachuting");
            changed = true;
        }

        private static void EnsureStateToMachineBoolTransition(
            AnimatorState source,
            AnimatorStateMachine destination,
            string parameter,
            ref bool changed)
        {
            AnimatorStateTransition[] transitions = source.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition != null &&
                    transition.destinationStateMachine == destination &&
                    HasCondition(
                        transition.conditions,
                        parameter,
                        AnimatorConditionMode.If))
                {
                    return;
                }
            }

            AnimatorStateTransition created = source.AddTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.08f;
            created.AddCondition(AnimatorConditionMode.If, 0f, parameter);
            changed = true;
        }

        private static bool HasCondition(
            AnimatorCondition[] conditions,
            string parameter,
            AnimatorConditionMode mode)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].parameter == parameter &&
                    conditions[i].mode == mode)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFullBodyActionTransitions(
            AnimatorStateMachine root,
            AnimatorState empty,
            AnimatorStateMachine destination,
            int action,
            ref bool changed)
        {
            bool hasEntry = false;
            AnimatorStateTransition[] entries = empty.transitions;
            for (int i = 0; i < entries.Length; i++)
            {
                AnimatorStateTransition transition = entries[i];
                if (transition != null &&
                    transition.destinationStateMachine == destination &&
                    HasIntCondition(
                        transition.conditions,
                        "FullBodyAction",
                        AnimatorConditionMode.Equals,
                        action))
                {
                    hasEntry = true;
                    break;
                }
            }

            if (!hasEntry)
            {
                AnimatorStateTransition entry = empty.AddTransition(destination);
                entry.hasExitTime = false;
                entry.hasFixedDuration = true;
                entry.duration = 0.08f;
                entry.AddCondition(
                    AnimatorConditionMode.Equals,
                    action,
                    "FullBodyAction"
                );
                changed = true;
            }

            AnimatorTransition[] exits =
                root.GetStateMachineTransitions(destination);
            for (int i = 0; i < exits.Length; i++)
            {
                AnimatorTransition transition = exits[i];
                if (transition != null &&
                    transition.destinationState == empty &&
                    HasIntCondition(
                        transition.conditions,
                        "FullBodyAction",
                        AnimatorConditionMode.NotEqual,
                        action))
                {
                    return;
                }
            }

            AnimatorTransition exit =
                root.AddStateMachineTransition(destination, empty);
            exit.AddCondition(
                AnimatorConditionMode.NotEqual,
                action,
                "FullBodyAction"
            );
            changed = true;
        }

        private static bool HasIntCondition(
            AnimatorCondition[] conditions,
            string parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].parameter == parameter &&
                    conditions[i].mode == mode &&
                    Mathf.Approximately(conditions[i].threshold, threshold))
                {
                    return true;
                }
            }

            return false;
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
