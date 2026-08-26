using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 07 del Animator ROS Classic.
    ///
    /// Materializa Base_Locomotion/Airborne con:
    /// Jump Start -> Jump Rise -> Fall -> Land.
    ///
    /// Entrada desde Grounded:
    /// - Si IsGrounded = false y VerticalVelocity > 0.10, entra por Jump Start.
    /// - Si IsGrounded = false y no hay ascenso, entra directamente por Fall.
    ///
    /// El repositorio tiene un unico clip de salto. Para mantener separados los
    /// estados conceptuales Jump Start y Jump Rise sin inventar una animacion,
    /// ambos reutilizan ese mismo clip: Jump Rise comienza aproximadamente en el
    /// 35% mediante cycleOffset. El valor queda visible y puede afinarse a mano.
    ///
    /// Fall usa Falling Idle, que ya esta importado como loop. Land usa
    /// Falling To Landing. Hard Landing queda reservado para una ampliacion
    /// posterior de aterrizajes Soft / Normal / Hard.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicAirborne
    {
        private const string JumpClipPath =
            "Assets/_Game/Animations/Character Animator/02. Jump - Air/Ch28_nonPBR@Jump.fbx";

        private const string FallingIdleClipPath =
            "Assets/_Game/Animations/Character Animator/02. Jump - Air/Ch28_nonPBR@Falling Idle.fbx";

        private const string FallingToLandingClipPath =
            "Assets/_Game/Animations/Character Animator/02. Jump - Air/Ch28_nonPBR@Falling To Landing.fbx";

        private const string HardLandingClipPath =
            "Assets/_Game/Animations/Character Animator/02. Jump - Air/Ch28_nonPBR@Hard Landing.fbx";

        private const float JumpEntryVelocity = 0.10f;
        private const float DescendingVelocity = -0.01f;
        private const float JumpSplitNormalizedTime = 0.35f;

        static EditorFirstRosClassicAirborne()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/07 - Construir Airborne")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            // Paso 06 ya fuerza toda la secuencia 01 -> 05 antes de construir
            // Prone. De esta forma 07 siempre parte de una arquitectura completa.
            EditorFirstRosClassicProne.Materialize();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            AnimatorControllerLayer baseLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.BaseLocomotionLayer
            );

            if (baseLayer == null || baseLayer.stateMachine == null)
                return;

            AnimatorStateMachine root = baseLayer.stateMachine;
            AnimatorStateMachine grounded = FindChildStateMachine(root, "Grounded");

            if (grounded == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No existe Grounded. " +
                    "Ejecuta primero '01 - Crear arquitectura base'."
                );
                return;
            }

            // Idempotente: una vez creado, no reconstruimos ni pisamos ajustes
            // manuales hechos desde el Animator.
            if (FindChildStateMachine(root, "Airborne") != null)
                return;

            AnimationClip jumpClip = LoadFirstAnimationClip(JumpClipPath);
            AnimationClip fallingIdleClip = LoadFirstAnimationClip(FallingIdleClipPath);
            AnimationClip fallingToLandingClip =
                LoadFirstAnimationClip(FallingToLandingClipPath);
            AnimationClip hardLandingClip = LoadFirstAnimationClip(HardLandingClipPath);

            AnimatorStateMachine airborne = root.AddStateMachine(
                "Airborne",
                new Vector3(690f, 80f, 0f)
            );

            AnimatorState jumpStart = airborne.AddState(
                "Jump Start",
                new Vector3(250f, -60f, 0f)
            );
            jumpStart.motion = jumpClip;
            jumpStart.speed = 1f;
            jumpStart.cycleOffset = 0f;
            jumpStart.writeDefaultValues = false;

            AnimatorState jumpRise = airborne.AddState(
                "Jump Rise",
                new Vector3(500f, -60f, 0f)
            );
            jumpRise.motion = jumpClip;
            jumpRise.speed = 1f;
            jumpRise.cycleOffset = JumpSplitNormalizedTime;
            jumpRise.writeDefaultValues = false;

            AnimatorState fall = airborne.AddState(
                "Fall",
                new Vector3(500f, 90f, 0f)
            );
            fall.motion = fallingIdleClip;
            fall.writeDefaultValues = false;

            AnimatorState land = airborne.AddState(
                "Land",
                new Vector3(760f, 90f, 0f)
            );
            land.motion = fallingToLandingClip;
            land.writeDefaultValues = false;

            // Una caida desde borde debe entrar a Fall. El salto solo sustituye
            // ese default cuando realmente hay velocidad vertical positiva.
            airborne.defaultState = fall;

            AnimatorTransition entryToJump = airborne.AddEntryTransition(jumpStart);
            entryToJump.AddCondition(
                AnimatorConditionMode.Greater,
                JumpEntryVelocity,
                "VerticalVelocity"
            );

            ConfigureGroundedExitPath(grounded);

            // Grounded sale primero por su Exit y el root selecciona Airborne.
            // Esto conserva la jerarquia Grounded/Airborne y evita Any State.
            AnimatorTransition groundedToAirborne =
                root.AddStateMachineTransition(grounded, airborne);
            groundedToAirborne.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "IsGrounded"
            );

            // Si el salto ya comenzo a descender antes del punto de division,
            // evitamos pasar por Jump Rise y entramos directamente a Fall.
            AnimatorStateTransition jumpStartToFall = jumpStart.AddTransition(fall);
            ConfigureTransition(jumpStartToFall, 0.04f);
            jumpStartToFall.AddCondition(
                AnimatorConditionMode.Less,
                DescendingVelocity,
                "VerticalVelocity"
            );

            // El unico clip Jump se divide visualmente en dos estados. Al entrar
            // a Jump Rise, cycleOffset continua aproximadamente desde este punto.
            AnimatorStateTransition jumpStartToRise = jumpStart.AddTransition(jumpRise);
            ConfigureExitTimeTransition(
                jumpStartToRise,
                JumpSplitNormalizedTime,
                0.03f
            );
            jumpStartToRise.AddCondition(
                AnimatorConditionMode.Greater,
                0f,
                "VerticalVelocity"
            );

            AnimatorStateTransition jumpRiseToFall = jumpRise.AddTransition(fall);
            ConfigureTransition(jumpRiseToFall, 0.05f);
            jumpRiseToFall.AddCondition(
                AnimatorConditionMode.Less,
                DescendingVelocity,
                "VerticalVelocity"
            );

            // Landing solo puede alcanzarse desde Fall.
            AnimatorStateTransition fallToLand = fall.AddTransition(land);
            ConfigureTransition(fallToLand, 0.04f);
            fallToLand.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "IsGrounded"
            );

            // Reproduce el aterrizaje y luego sale de Airborne.
            AnimatorStateTransition landToExit = land.AddExitTransition();
            ConfigureExitTimeTransition(landToExit, 0.90f, 0.06f);

            // La salida del sub-state machine vuelve a Grounded. Grounded ya
            // conserva su propia logica de Standing/Crouch/Prone.
            root.AddStateMachineTransition(airborne, grounded);

            EditorUtility.SetDirty(jumpStart);
            EditorUtility.SetDirty(jumpRise);
            EditorUtility.SetDirty(fall);
            EditorUtility.SetDirty(land);
            EditorUtility.SetDirty(airborne);
            EditorUtility.SetDirty(grounded);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Airborne creado: " +
                "Jump Start -> Jump Rise -> Fall -> Land. " +
                "Las caidas desde borde entran directamente por Fall."
            );

            if (jumpClip == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se encontro el clip Jump: " +
                    JumpClipPath
                );
            }
            else
            {
                Debug.Log(
                    "[ROS Classic Animator] Jump Start y Jump Rise reutilizan el " +
                    "unico clip Jump disponible. Jump Rise comienza con cycleOffset=" +
                    JumpSplitNormalizedTime.ToString("0.00") + "."
                );
            }

            if (fallingIdleClip == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se encontro Falling Idle: " +
                    FallingIdleClipPath
                );
            }

            if (fallingToLandingClip == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se encontro Falling To Landing: " +
                    FallingToLandingClipPath
                );
            }

            if (hardLandingClip != null)
            {
                Debug.Log(
                    "[ROS Classic Animator] Hard Landing existe y queda reservado " +
                    "para una fase posterior de Land Soft / Normal / Hard."
                );
            }
        }

        /// <summary>
        /// Grounded contiene sub-state machines (Standing/Crouch/Prone). Para
        /// saltar a un state machine hermano, primero cada postura debe salir a
        /// Grounded/Exit y despues Grounded sale al root. No usamos Any State,
        /// porque podria reentrar Airborne mientras IsGrounded siga en false.
        /// </summary>
        private static void ConfigureGroundedExitPath(AnimatorStateMachine grounded)
        {
            ChildAnimatorStateMachine[] stanceChildren = grounded.stateMachines;
            for (int i = 0; i < stanceChildren.Length; i++)
            {
                AnimatorStateMachine stance = stanceChildren[i].stateMachine;
                if (stance == null || !IsGroundedStanceMachine(stance.name))
                    continue;

                AddAirborneExitToLeafStates(stance);
                GuardGroundedStateMachineTransitions(grounded, stance);

                AnimatorTransition stanceToGroundedExit =
                    grounded.AddStateMachineExitTransition(stance);
                stanceToGroundedExit.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsGrounded"
                );
            }
        }

        private static bool IsGroundedStanceMachine(string name)
        {
            return name == "Standing" || name == "Crouch" || name == "Prone";
        }

        private static void AddAirborneExitToLeafStates(AnimatorStateMachine machine)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null)
                    continue;

                AnimatorStateTransition exit = state.AddExitTransition();
                ConfigureTransition(exit, 0.02f);
                exit.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsGrounded"
                );
                EditorUtility.SetDirty(state);
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null)
                    AddAirborneExitToLeafStates(child);
            }
        }

        /// <summary>
        /// Si el jugador cambia Stance exactamente al perder el suelo, Airborne
        /// debe tener prioridad. Agregamos IsGrounded=true a las transiciones
        /// Standing/Crouch/Prone para que solo compitan mientras hay suelo.
        /// </summary>
        private static void GuardGroundedStateMachineTransitions(
            AnimatorStateMachine grounded,
            AnimatorStateMachine source)
        {
            AnimatorTransition[] transitions =
                grounded.GetStateMachineTransitions(source);

            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorTransition transition = transitions[i];
                if (transition == null || transition.isExit)
                    continue;

                if (!HasCondition(
                        transition.conditions,
                        "IsGrounded",
                        AnimatorConditionMode.If))
                {
                    transition.AddCondition(
                        AnimatorConditionMode.If,
                        0f,
                        "IsGrounded"
                    );
                    EditorUtility.SetDirty(transition);
                }
            }
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
            if (parent == null)
                return null;

            ChildAnimatorStateMachine[] children = parent.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private static void ConfigureTransition(
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

        private static void ConfigureExitTimeTransition(
            AnimatorStateTransition transition,
            float exitTime,
            float duration)
        {
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null ||
                    clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return clip;
            }

            return null;
        }
    }
}
