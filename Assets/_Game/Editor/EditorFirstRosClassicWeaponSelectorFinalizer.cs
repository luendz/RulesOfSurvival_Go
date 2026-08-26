using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Compilation;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Finalizador robusto del selector de familias de UpperBody_Weapon.
    ///
    /// Problema que resuelve:
    /// WeaponType puede cambiar correctamente en runtime, pero una sub-state
    /// machine (Unarmed/Rifle/Pistol/Shotgun/...) permanece activa si sus estados
    /// internos no tienen una salida hacia el parent cuando WeaponType deja de
    /// coincidir con la familia actual.
    ///
    /// Este finalizador garantiza en un solo paso:
    /// 1) Weapon Selector -> cada familia por WeaponType == valor.
    /// 2) Cada familia -> las otras familias por WeaponType == valor destino.
    /// 3) Cada estado interno -> Exit cuando WeaponType != valor de su familia.
    ///
    /// No asigna, reemplaza ni modifica Motion/AnimationClip.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicWeaponSelectorFinalizer
    {
        private const string WeaponType = "WeaponType";

        private static readonly string[] FamilyNames =
        {
            "Unarmed",
            "Rifle",
            "Pistol",
            "Shotgun",
            "Sniper",
            "Melee",
            "Throwable"
        };

        private static readonly int[] FamilyValues =
        {
            0, 1, 2, 3, 4, 5, 6
        };

        static EditorFirstRosClassicWeaponSelectorFinalizer()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            QueueFinalize();
        }

        private static void OnCompilationFinished(object context)
        {
            QueueFinalize();
        }

        private static void QueueFinalize()
        {
            EditorApplication.delayCall -= TryFinalizeAfterEditorIsReady;
            EditorApplication.delayCall += TryFinalizeAfterEditorIsReady;
        }

        private static void TryFinalizeAfterEditorIsReady()
        {
            if (Application.isPlaying)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueFinalize();
                return;
            }

            FinalizeSelector();
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/95 - Finalizar selector WeaponType")]
        public static void FinalizeSelector()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            EnsureParameter(
                controller,
                WeaponType,
                AnimatorControllerParameterType.Int
            );

            AnimatorControllerLayer weaponLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyWeaponLayer
            );

            if (weaponLayer == null || weaponLayer.stateMachine == null)
                return;

            AnimatorStateMachine root = weaponLayer.stateMachine;
            AnimatorState selector = FindState(root, "Weapon Selector");

            if (selector == null)
            {
                Debug.LogError(
                    "[ROS Classic Animator] No existe 'Weapon Selector' en " +
                    "UpperBody_Weapon. Ejecuta primero el paso 08."
                );
                return;
            }

            AnimatorStateMachine[] families =
                new AnimatorStateMachine[FamilyNames.Length];

            bool changed = false;

            for (int i = 0; i < FamilyNames.Length; i++)
            {
                families[i] = FindChildStateMachine(root, FamilyNames[i]);

                if (families[i] == null)
                {
                    Debug.LogWarning(
                        "[ROS Classic Animator] Falta la familia '" +
                        FamilyNames[i] + "' en UpperBody_Weapon."
                    );
                    continue;
                }

                changed |= EnsureSelectorTransition(
                    selector,
                    families[i],
                    FamilyValues[i]
                );

                changed |= EnsureExitTransitionsRecursive(
                    families[i],
                    FamilyValues[i]
                );
            }

            for (int from = 0; from < families.Length; from++)
            {
                AnimatorStateMachine source = families[from];
                if (source == null)
                    continue;

                for (int to = 0; to < families.Length; to++)
                {
                    if (from == to)
                        continue;

                    AnimatorStateMachine destination = families[to];
                    if (destination == null)
                        continue;

                    changed |= EnsureFamilyTransition(
                        root,
                        source,
                        destination,
                        FamilyValues[to]
                    );
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(selector);
                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                "[ROS Classic Animator] Selector WeaponType verificado. " +
                "Unarmed=0, Rifle=1, Pistol=2, Shotgun=3, Sniper=4, " +
                "Melee=5, Throwable=6. Cada estado interno tiene salida " +
                "cuando WeaponType deja de coincidir con su familia."
            );
        }

        private static bool EnsureSelectorTransition(
            AnimatorState selector,
            AnimatorStateMachine destination,
            int weaponType)
        {
            AnimatorStateTransition[] transitions = selector.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null ||
                    transition.destinationStateMachine != destination)
                {
                    continue;
                }

                if (HasCondition(
                        transition.conditions,
                        WeaponType,
                        AnimatorConditionMode.Equals,
                        weaponType))
                {
                    return false;
                }
            }

            AnimatorStateTransition created =
                selector.AddTransition(destination);
            ConfigureStateTransition(created, 0.03f);
            created.AddCondition(
                AnimatorConditionMode.Equals,
                weaponType,
                WeaponType
            );
            return true;
        }

        private static bool EnsureFamilyTransition(
            AnimatorStateMachine root,
            AnimatorStateMachine source,
            AnimatorStateMachine destination,
            int destinationWeaponType)
        {
            AnimatorTransition[] transitions =
                root.GetStateMachineTransitions(source);

            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorTransition transition = transitions[i];
                if (transition == null ||
                    transition.destinationStateMachine != destination)
                {
                    continue;
                }

                if (HasCondition(
                        transition.conditions,
                        WeaponType,
                        AnimatorConditionMode.Equals,
                        destinationWeaponType))
                {
                    return false;
                }
            }

            AnimatorTransition created =
                root.AddStateMachineTransition(source, destination);
            created.AddCondition(
                AnimatorConditionMode.Equals,
                destinationWeaponType,
                WeaponType
            );
            return true;
        }

        private static bool EnsureExitTransitionsRecursive(
            AnimatorStateMachine family,
            int familyWeaponType)
        {
            bool changed = false;

            ChildAnimatorState[] states = family.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null)
                    continue;

                if (HasExitForDifferentWeaponType(state, familyWeaponType))
                    continue;

                AnimatorStateTransition exit = state.AddExitTransition();
                ConfigureStateTransition(exit, 0.02f);
                exit.interruptionSource =
                    TransitionInterruptionSource.SourceThenDestination;
                exit.orderedInterruption = false;
                exit.AddCondition(
                    AnimatorConditionMode.NotEqual,
                    familyWeaponType,
                    WeaponType
                );

                EditorUtility.SetDirty(state);
                changed = true;
            }

            ChildAnimatorStateMachine[] children = family.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null)
                {
                    changed |= EnsureExitTransitionsRecursive(
                        child,
                        familyWeaponType
                    );
                }
            }

            if (changed)
                EditorUtility.SetDirty(family);

            return changed;
        }

        private static bool HasExitForDifferentWeaponType(
            AnimatorState state,
            int familyWeaponType)
        {
            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null || !transition.isExit)
                    continue;

                if (HasCondition(
                        transition.conditions,
                        WeaponType,
                        AnimatorConditionMode.NotEqual,
                        familyWeaponType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCondition(
            AnimatorCondition[] conditions,
            string parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                AnimatorCondition condition = conditions[i];
                if (condition.parameter == parameter &&
                    condition.mode == mode &&
                    Mathf.Approximately(condition.threshold, threshold))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigureStateTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
            transition.exitTime = 0f;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
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

        private static AnimatorStateMachine FindChildStateMachine(
            AnimatorStateMachine parent,
            string name)
        {
            ChildAnimatorStateMachine[] children = parent.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == name)
                    return state;
            }

            return null;
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }
    }
}
