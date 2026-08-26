using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 06 del Animator ROS Classic.
    ///
    /// Materializa Grounded/Prone con:
    /// Prone Enter -> Prone Idle / Prone Crawl_2D -> Prone Exit.
    ///
    /// Usa Stance = 2 para Prone. El repositorio no contiene actualmente clips
    /// dedicados de Prone/Crawl, por lo que los estados se crean visibles con
    /// Motion=None y el Blend Tree deja sus 8 direcciones vacias. No se reutilizan
    /// clips de Crouch ni se fabrican movimientos falsos.
    ///
    /// Mientras no existan clips Enter/Exit, esas fases avanzan inmediatamente
    /// para evitar que un estado Motion=None bloquee el flujo del Animator.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicProne
    {
        private const int StandingStance = 0;
        private const int CrouchStance = 1;
        private const int ProneStance = 2;
        private const float MoveThreshold = 0.05f;

        static EditorFirstRosClassicProne()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/06 - Construir Prone")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            // Fuerza la secuencia 01 -> 05 antes de Prone. Todos los pasos son
            // idempotentes, asi que si ya existen no pisan cambios manuales.
            EditorFirstRosClassicAnimatorBuilder.CreateIfMissing();
            EditorFirstRosClassicAnimatorBuilder.BuildWalk8Directions();
            EditorFirstRosClassicRun2D.Materialize();
            EditorFirstRosClassicSprint.Materialize();
            EditorFirstRosClassicCrouch.Materialize();

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

            AnimatorStateMachine grounded =
                FindChildStateMachine(baseLayer.stateMachine, "Grounded");

            if (grounded == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No existe Grounded. " +
                    "Ejecuta primero '01 - Crear arquitectura base'."
                );
                return;
            }

            AnimatorStateMachine standing =
                FindChildStateMachine(grounded, "Standing");
            AnimatorStateMachine crouch =
                FindChildStateMachine(grounded, "Crouch");

            if (standing == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No existe Grounded/Standing."
                );
                return;
            }

            if (FindChildStateMachine(grounded, "Prone") != null)
                return;

            AnimatorStateMachine prone = grounded.AddStateMachine(
                "Prone",
                new Vector3(940f, 330f, 0f)
            );

            AnimatorState proneEnter = prone.AddState(
                "Prone Enter",
                new Vector3(250f, 20f, 0f)
            );
            proneEnter.motion = null;
            proneEnter.writeDefaultValues = false;

            AnimatorState proneIdle = prone.AddState(
                "Prone Idle",
                new Vector3(510f, -50f, 0f)
            );
            proneIdle.motion = null;
            proneIdle.writeDefaultValues = false;

            BlendTree proneCrawlTree = new BlendTree
            {
                name = "BT_Prone_Crawl_8D",
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(proneCrawlTree, controller);

            // Layout direccional oficial. Se deja completamente vacio porque no
            // hay clips Prone/Crawl reales en el repositorio actual.
            proneCrawlTree.AddChild(null, new Vector2(0f, 1f));
            proneCrawlTree.AddChild(null, new Vector2(-1f, 1f));
            proneCrawlTree.AddChild(null, new Vector2(-1f, 0f));
            proneCrawlTree.AddChild(null, new Vector2(-1f, -1f));
            proneCrawlTree.AddChild(null, new Vector2(0f, -1f));
            proneCrawlTree.AddChild(null, new Vector2(1f, -1f));
            proneCrawlTree.AddChild(null, new Vector2(1f, 0f));
            proneCrawlTree.AddChild(null, new Vector2(1f, 1f));

            AnimatorState proneCrawl = prone.AddState(
                "Prone Crawl_2D",
                new Vector3(510f, 100f, 0f)
            );
            proneCrawl.motion = proneCrawlTree;
            proneCrawl.writeDefaultValues = false;

            AnimatorState proneExit = prone.AddState(
                "Prone Exit",
                new Vector3(780f, 20f, 0f)
            );
            proneExit.motion = null;
            proneExit.writeDefaultValues = false;

            prone.defaultState = proneEnter;

            // Standing -> Prone cuando Stance = 2.
            AddStandingExitTransition(FindState(standing, "Idle"));
            AddStandingExitTransition(FindState(standing, "Walk_2D"));
            AddStandingExitTransition(FindState(standing, "Run_2D"));
            AddStandingExitTransition(FindState(standing, "Sprint"));

            AnimatorTransition standingToProne =
                grounded.AddStateMachineTransition(standing, prone);
            standingToProne.AddCondition(
                AnimatorConditionMode.Equals,
                ProneStance,
                "Stance"
            );

            // Crouch ya existia antes que Prone y originalmente tenia una salida
            // incondicional a Standing. La convertimos en Stance=0 y agregamos la
            // ruta directa Stance=2 para respetar Crouch -> Prone.
            if (crouch != null)
                EnsureCrouchRoutes(grounded, crouch, standing, prone);

            // Sin clip de entrada, avanzamos inmediatamente a Idle. Si el usuario
            // cambia Stance durante esa entrada vacia, se cancela hacia Exit.
            AnimatorStateTransition enterToIdle = proneEnter.AddTransition(proneIdle);
            ConfigureTransition(enterToIdle, 0.01f);
            enterToIdle.AddCondition(
                AnimatorConditionMode.Equals,
                ProneStance,
                "Stance"
            );

            AnimatorStateTransition enterCancel = proneEnter.AddTransition(proneExit);
            ConfigureTransition(enterCancel, 0.01f);
            enterCancel.AddCondition(
                AnimatorConditionMode.NotEqual,
                ProneStance,
                "Stance"
            );

            // Idle <-> Crawl. El arbol ya queda listo para recibir los 8 clips.
            AnimatorStateTransition idleToCrawl = proneIdle.AddTransition(proneCrawl);
            ConfigureTransition(idleToCrawl, 0.10f);
            idleToCrawl.AddCondition(
                AnimatorConditionMode.Greater,
                MoveThreshold,
                "Speed"
            );

            AnimatorStateTransition crawlToIdle = proneCrawl.AddTransition(proneIdle);
            ConfigureTransition(crawlToIdle, 0.10f);
            crawlToIdle.AddCondition(
                AnimatorConditionMode.Less,
                MoveThreshold,
                "Speed"
            );

            AddExitToProneExit(proneIdle, proneExit);
            AddExitToProneExit(proneCrawl, proneExit);

            // No existe aun un clip Prone Exit: salimos inmediatamente al parent.
            AnimatorStateTransition exitToParent = proneExit.AddExitTransition();
            ConfigureTransition(exitToParent, 0.01f);

            // Prone puede volver a Standing o Crouch segun Stance.
            AnimatorTransition proneToStanding =
                grounded.AddStateMachineTransition(prone, standing);
            proneToStanding.AddCondition(
                AnimatorConditionMode.Equals,
                StandingStance,
                "Stance"
            );

            if (crouch != null)
            {
                AnimatorTransition proneToCrouch =
                    grounded.AddStateMachineTransition(prone, crouch);
                proneToCrouch.AddCondition(
                    AnimatorConditionMode.Equals,
                    CrouchStance,
                    "Stance"
                );
            }

            EditorUtility.SetDirty(proneCrawlTree);
            EditorUtility.SetDirty(proneEnter);
            EditorUtility.SetDirty(proneIdle);
            EditorUtility.SetDirty(proneCrawl);
            EditorUtility.SetDirty(proneExit);
            EditorUtility.SetDirty(prone);
            EditorUtility.SetDirty(grounded);
            EditorUtility.SetDirty(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Prone creado: " +
                "Enter -> Idle/Crawl_2D -> Exit. Stance=2 activa Prone."
            );

            Debug.LogWarning(
                "[ROS Classic Animator] No hay clips Prone/Crawl en los assets actuales. " +
                "Prone Enter, Idle, Exit y las 8 direcciones de Crawl quedan visibles " +
                "como Motion=None para asignarlas manualmente cuando existan."
            );
        }

        private static void EnsureCrouchRoutes(
            AnimatorStateMachine grounded,
            AnimatorStateMachine crouch,
            AnimatorStateMachine standing,
            AnimatorStateMachine prone)
        {
            AnimatorTransition[] transitions =
                grounded.GetStateMachineTransitions(crouch);
            bool hasCrouchToProne = false;

            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorTransition transition = transitions[i];
                if (transition == null)
                    continue;

                if (transition.destinationStateMachine == prone)
                {
                    hasCrouchToProne = true;
                    continue;
                }

                if (transition.destinationStateMachine == standing &&
                    transition.conditions.Length == 0)
                {
                    transition.AddCondition(
                        AnimatorConditionMode.Equals,
                        StandingStance,
                        "Stance"
                    );
                    EditorUtility.SetDirty(transition);
                }
            }

            if (!hasCrouchToProne)
            {
                AnimatorTransition crouchToProne =
                    grounded.AddStateMachineTransition(crouch, prone);
                crouchToProne.AddCondition(
                    AnimatorConditionMode.Equals,
                    ProneStance,
                    "Stance"
                );
                EditorUtility.SetDirty(crouchToProne);
            }
        }

        private static void AddStandingExitTransition(AnimatorState state)
        {
            if (state == null)
                return;

            AnimatorStateTransition transition = state.AddExitTransition();
            ConfigureTransition(transition, 0.05f);
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                ProneStance,
                "Stance"
            );
        }

        private static void AddExitToProneExit(
            AnimatorState from,
            AnimatorState proneExit)
        {
            if (from == null || proneExit == null)
                return;

            AnimatorStateTransition transition = from.AddTransition(proneExit);
            ConfigureTransition(transition, 0.05f);
            transition.AddCondition(
                AnimatorConditionMode.NotEqual,
                ProneStance,
                "Stance"
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
    }
}
