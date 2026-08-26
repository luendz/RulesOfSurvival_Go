using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Construye el Animator ROS Classic como un asset independiente para poder
    /// migrar por fases sin romper AC_Player_Prototype durante la transición.
    ///
    /// Fase inicial:
    /// - Crea las 6 capas oficiales de la arquitectura ROS Classic.
    /// - Crea los parámetros oficiales.
    /// - Crea máscaras editables separadas para UpperBody, Aim y Lean.
    /// - Implementa el primer punto del roadmap: Grounded/Standing/Idle.
    ///
    /// Si el controller ya existe, no lo reconstruye para respetar cualquier
    /// ajuste manual realizado posteriormente desde el Animator.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicAnimatorBuilder
    {
        public const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_ROS_Classic.controller";

        private const string SourceUpperBodyMaskPath =
            "Assets/_Game/Animations/Masks/AM_WeaponUpperBody.mask";

        private const string UpperBodyMaskPath =
            "Assets/_Game/Animations/Masks/AM_ROS_UpperBody.mask";

        private const string AimMaskPath =
            "Assets/_Game/Animations/Masks/AM_ROS_Aim.mask";

        private const string LeanMaskPath =
            "Assets/_Game/Animations/Masks/AM_ROS_Lean.mask";

        private const string IdleClipPath =
            "Assets/_Game/Animations/Character Animator/01. Locomotion/Idle/Ch28_nonPBR@Idle.fbx";

        public const string BaseLocomotionLayer = "Base_Locomotion";
        public const string UpperBodyWeaponLayer = "UpperBody_Weapon";
        public const string UpperBodyActionsLayer = "UpperBody_Actions";
        public const string AimOffsetLayer = "Aim_Offset";
        public const string LeanLayer = "Lean";
        public const string FullBodyActionsLayer = "FullBody_Actions";

        static EditorFirstRosClassicAnimatorBuilder()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/01 - Crear arquitectura base")]
        public static void CreateIfMissing()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController existing =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
                return;

            EnsureMaskCopy(UpperBodyMaskPath);
            EnsureMaskCopy(AimMaskPath);
            EnsureMaskCopy(LeanMaskPath);

            AvatarMask upperBodyMask =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            AvatarMask aimMask =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(AimMaskPath);
            AvatarMask leanMask =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(LeanMaskPath);

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[ROS Classic Animator] No se pudo crear " + ControllerPath);
                return;
            }

            ConfigureParameters(controller);
            ConfigureLayers(controller, upperBodyMask, aimMask, leanMask);
            BuildStandingIdle(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Arquitectura base creada en AC_Player_ROS_Classic: " +
                "6 layers + Grounded/Standing/Idle. El controller activo anterior no fue reemplazado."
            );
        }

        private static void ConfigureLayers(
            AnimatorController controller,
            AvatarMask upperBodyMask,
            AvatarMask aimMask,
            AvatarMask leanMask)
        {
            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = BaseLocomotionLayer;
            baseLayer.stateMachine.name = BaseLocomotionLayer;
            baseLayer.avatarMask = null;
            baseLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            baseLayer.defaultWeight = 1f;
            baseLayer.iKPass = false;

            controller.layers = new[] { baseLayer };

            AddLayer(
                controller,
                UpperBodyWeaponLayer,
                upperBodyMask,
                AnimatorLayerBlendingMode.Override,
                1f,
                true
            );

            AddLayer(
                controller,
                UpperBodyActionsLayer,
                upperBodyMask,
                AnimatorLayerBlendingMode.Override,
                0f,
                true
            );

            AddLayer(
                controller,
                AimOffsetLayer,
                aimMask,
                AnimatorLayerBlendingMode.Additive,
                1f,
                false
            );

            AddLayer(
                controller,
                LeanLayer,
                leanMask,
                AnimatorLayerBlendingMode.Additive,
                1f,
                false
            );

            AddLayer(
                controller,
                FullBodyActionsLayer,
                null,
                AnimatorLayerBlendingMode.Override,
                0f,
                false
            );
        }

        private static void AddLayer(
            AnimatorController controller,
            string name,
            AvatarMask mask,
            AnimatorLayerBlendingMode blendingMode,
            float defaultWeight,
            bool ikPass)
        {
            controller.AddLayer(name);

            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer layer = layers[layers.Length - 1];
            layer.name = name;
            layer.stateMachine.name = name;
            layer.avatarMask = mask;
            layer.blendingMode = blendingMode;
            layer.defaultWeight = defaultWeight;
            layer.iKPass = ikPass;

            AnimatorState empty = layer.stateMachine.AddState(
                "Empty",
                new Vector3(260f, 40f, 0f)
            );
            empty.writeDefaultValues = false;
            layer.stateMachine.defaultState = empty;

            layers[layers.Length - 1] = layer;
            controller.layers = layers;
        }

        private static void BuildStandingIdle(AnimatorController controller)
        {
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine root = baseLayer.stateMachine;

            // CreateAnimatorControllerAtPath agrega un estado vacío inicial.
            // Lo retiramos porque esta arquitectura empieza en Grounded/Standing.
            ChildAnimatorState[] initialStates = root.states;
            for (int i = initialStates.Length - 1; i >= 0; i--)
            {
                if (initialStates[i].state != null)
                    root.RemoveState(initialStates[i].state);
            }

            AnimatorStateMachine grounded = root.AddStateMachine(
                "Grounded",
                new Vector3(360f, 80f, 0f)
            );

            AnimatorStateMachine standing = grounded.AddStateMachine(
                "Standing",
                new Vector3(360f, 80f, 0f)
            );

            AnimationClip idleClip = LoadFirstAnimationClip(IdleClipPath);
            AnimatorState idle = standing.AddState(
                "Idle",
                new Vector3(360f, 80f, 0f)
            );
            idle.motion = idleClip;
            idle.writeDefaultValues = false;
            standing.defaultState = idle;

            root.AddEntryTransition(grounded);
            grounded.AddEntryTransition(standing);

            if (idleClip == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Standing/Idle fue creado, pero no se encontró el clip: " +
                    IdleClipPath
                );
            }
        }

        private static void ConfigureParameters(AnimatorController controller)
        {
            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "VerticalVelocity", AnimatorControllerParameterType.Float);

            EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsSprinting", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsAutoRunning", AnimatorControllerParameterType.Bool);

            EnsureParameter(controller, "Stance", AnimatorControllerParameterType.Int);

            EnsureParameter(controller, "WeaponType", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "IsAiming", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsFiring", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsReloading", AnimatorControllerParameterType.Bool);

            EnsureParameter(controller, "AimPitch", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "AimYaw", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Lean", AnimatorControllerParameterType.Float);

            EnsureParameter(controller, "IsVaulting", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsSwimming", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsUnderwater", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsParachuting", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsFreeFalling", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsKnocked", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "FullBodyAction", AnimatorControllerParameterType.Int);
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

        private static void EnsureMaskCopy(string destinationPath)
        {
            if (AssetDatabase.LoadAssetAtPath<AvatarMask>(destinationPath) != null)
                return;

            AvatarMask source =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(SourceUpperBodyMaskPath);
            if (source == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se encontró la máscara base: " +
                    SourceUpperBodyMaskPath
                );
                return;
            }

            if (!AssetDatabase.CopyAsset(SourceUpperBodyMaskPath, destinationPath))
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se pudo crear la máscara: " +
                    destinationPath
                );
            }
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
