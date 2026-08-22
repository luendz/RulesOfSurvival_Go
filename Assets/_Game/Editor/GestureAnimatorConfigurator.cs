using System;
using System.Linq;
using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.Editor
{
    [InitializeOnLoad]
    public static class GestureAnimatorConfigurator
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        internal const string GestureFolderPrefix =
            "Assets/_Game/Animations/Character/Gestures/";

        private readonly struct GestureAsset
        {
            public GestureAsset(
                string fileName,
                string clipName,
                string stateName
            )
            {
                FileName = fileName;
                ClipName = clipName;
                StateName = stateName;
            }

            public string FileName { get; }
            public string ClipName { get; }
            public string StateName { get; }

            public string AssetPath =>
                GestureFolderPrefix + FileName;
        }

        private static readonly GestureAsset[] Gestures =
        {
            new GestureAsset(
                "Ch28_nonPBR@Dancing.fbx",
                "Dancing",
                "Gesture_Dancing"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Fishing Cast.fbx",
                "Fishing Cast",
                "Gesture_Fishing_Cast"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Hip Hop Dancing.fbx",
                "Hip Hop Dancing",
                "Gesture_Hip_Hop_Dancing"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Joyful Jump.fbx",
                "Joyful Jump",
                "Gesture_Joyful_Jump"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Opening.fbx",
                "Opening",
                "Gesture_Opening"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Rumba Dancing.fbx",
                "Rumba Dancing",
                "Gesture_Rumba_Dancing"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Salute.fbx",
                "Salute",
                "Gesture_Salute"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Talking On Phone.fbx",
                "Talking On Phone",
                "Gesture_Talking_On_Phone"
            ),
            new GestureAsset(
                "Ch28_nonPBR@Waving Gesture.fbx",
                "Waving Gesture",
                "Gesture_Waving_Gesture"
            )
        };

        static GestureAnimatorConfigurator()
        {
            EditorApplication.delayCall +=
                ConfigureIfNeeded;
        }

        [MenuItem(
            "Tools/Rules of Survival/Configurar sistema de gestos"
        )]
        public static void ConfigureFromMenu()
        {
            ConfigureController(true);
        }

        internal static void ScheduleConfiguration()
        {
            EditorApplication.delayCall -=
                ConfigureIfNeeded;
            EditorApplication.delayCall +=
                ConfigureIfNeeded;
        }

        private static void ConfigureIfNeeded()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath
                );

            if (controller == null ||
                !NeedsConfiguration(controller))
            {
                return;
            }

            ConfigureController(false);
        }

        private static bool NeedsConfiguration(
            AnimatorController controller
        )
        {
            AnimatorControllerLayer layer =
                controller.layers.FirstOrDefault(
                    candidate =>
                        candidate.name ==
                        PlayerGestureController.GestureLayerName
                );

            if (layer == null || layer.stateMachine == null)
                return true;

            AnimatorStateMachine stateMachine =
                layer.stateMachine;

            if (FindState(
                    stateMachine,
                    PlayerGestureController.GestureIdleState
                ) == null)
            {
                return true;
            }

            foreach (GestureAsset gesture in Gestures)
            {
                AnimatorState state =
                    FindState(
                        stateMachine,
                        gesture.StateName
                    );

                if (state == null || state.motion == null)
                    return true;
            }

            return false;
        }

        private static void ConfigureController(
            bool logSuccess
        )
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath
                );

            if (controller == null)
            {
                Debug.LogWarning(
                    $"No se encontró el Animator Controller en '{ControllerPath}'."
                );
                return;
            }

            AnimatorControllerLayer layer =
                EnsureGestureLayer(controller);

            AnimatorStateMachine stateMachine =
                layer.stateMachine;

            AnimatorState idleState =
                FindState(
                    stateMachine,
                    PlayerGestureController.GestureIdleState
                );

            if (idleState == null)
            {
                idleState = stateMachine.AddState(
                    PlayerGestureController.GestureIdleState,
                    new Vector3(300f, 40f, 0f)
                );
            }

            idleState.motion = null;
            idleState.writeDefaultValues = true;
            stateMachine.defaultState = idleState;

            bool configuredAny = false;
            int missingClips = 0;
            float sector = 360f / Gestures.Length;

            for (int i = 0; i < Gestures.Length; i++)
            {
                GestureAsset gesture = Gestures[i];

                AnimationClip clip =
                    LoadGestureClip(gesture);

                if (clip == null)
                {
                    missingClips++;
                    Debug.LogWarning(
                        $"No se pudo cargar el clip '{gesture.ClipName}' desde '{gesture.AssetPath}'."
                    );
                    continue;
                }

                AnimatorState state =
                    FindState(
                        stateMachine,
                        gesture.StateName
                    );

                if (state == null)
                {
                    float angle =
                        (90f - sector * i) *
                        Mathf.Deg2Rad;

                    Vector3 position = new Vector3(
                        600f + Mathf.Cos(angle) * 280f,
                        300f - Mathf.Sin(angle) * 220f,
                        0f
                    );

                    state = stateMachine.AddState(
                        gesture.StateName,
                        position
                    );
                }

                state.motion = clip;
                state.tag = "Gesture";
                state.speed = 1f;
                state.writeDefaultValues = true;

                foreach (
                    AnimatorStateTransition transition
                    in state.transitions.ToArray()
                )
                {
                    state.RemoveTransition(transition);
                }

                AnimatorStateTransition exitTransition =
                    state.AddTransition(idleState);

                exitTransition.hasExitTime = true;
                exitTransition.exitTime = 0.98f;
                exitTransition.duration = 0.12f;
                exitTransition.hasFixedDuration = true;
                exitTransition.canTransitionToSelf = false;

                configuredAny = true;
                EditorUtility.SetDirty(state);
            }

            AnimatorControllerLayer[] layers =
                controller.layers;

            int layerIndex = Array.FindIndex(
                layers,
                candidate =>
                    candidate.name ==
                    PlayerGestureController.GestureLayerName
            );

            if (layerIndex >= 0)
            {
                layers[layerIndex].defaultWeight = 1f;
                layers[layerIndex].blendingMode =
                    AnimatorLayerBlendingMode.Override;
                controller.layers = layers;
            }

            EditorUtility.SetDirty(idleState);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (logSuccess || configuredAny)
            {
                Debug.Log(
                    $"Sistema de gestos configurado en '{ControllerPath}'. " +
                    $"Gestos: {Gestures.Length - missingClips}/{Gestures.Length}."
                );
            }
        }

        private static AnimatorControllerLayer EnsureGestureLayer(
            AnimatorController controller
        )
        {
            AnimatorControllerLayer layer =
                controller.layers.FirstOrDefault(
                    candidate =>
                        candidate.name ==
                        PlayerGestureController.GestureLayerName
                );

            if (layer != null && layer.stateMachine != null)
                return layer;

            controller.AddLayer(
                PlayerGestureController.GestureLayerName
            );

            layer = controller.layers.First(
                candidate =>
                    candidate.name ==
                    PlayerGestureController.GestureLayerName
            );

            return layer;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine stateMachine,
            string stateName
        )
        {
            return stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(
                    state => state != null &&
                             state.name == stateName
                );
        }

        private static AnimationClip LoadGestureClip(
            GestureAsset gesture
        )
        {
            AnimationClip[] clips =
                AssetDatabase.LoadAllAssetsAtPath(
                    gesture.AssetPath
                )
                .OfType<AnimationClip>()
                .Where(
                    clip =>
                        clip != null &&
                        !clip.name.StartsWith(
                            "__preview__",
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                .ToArray();

            return clips.FirstOrDefault(
                       clip =>
                           string.Equals(
                               clip.name,
                               gesture.ClipName,
                               StringComparison.OrdinalIgnoreCase
                           )
                   ) ??
                   clips.FirstOrDefault();
        }
    }

    public sealed class GestureAnimatorAssetPostprocessor :
        AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            bool gestureAssetChanged =
                importedAssets.Any(IsGestureAsset) ||
                movedAssets.Any(IsGestureAsset);

            if (gestureAssetChanged)
            {
                GestureAnimatorConfigurator
                    .ScheduleConfiguration();
            }
        }

        private static bool IsGestureAsset(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.StartsWith(
                       GestureAnimatorConfigurator
                           .GestureFolderPrefix,
                       StringComparison.OrdinalIgnoreCase
                   );
        }
    }
}
