using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.Editor
{
    public static class PickupAnimationSetup
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string ClipAssetPath =
            "Assets/_Game/Animations/Character/Locomotion/Ch28_nonPBR@Taking Item.fbx";

        private const string TriggerName = "PickupItem";
        private const string StateName   = "TakingItem";
        private const string LayerName   = "Actions";
        private const float  Speed       = 3f;

        [MenuItem("ROS/Setup/Agregar animación de recogida (Taking Item)")]
        public static void Run()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
            {
                Debug.LogError(
                    $"[PickupAnimationSetup] No se encontró el controller en:\n{ControllerPath}"
                );
                return;
            }

            AnimationClip clip = LoadClip();
            if (clip == null)
            {
                Debug.LogError(
                    $"[PickupAnimationSetup] No se encontró AnimationClip en:\n{ClipAssetPath}"
                );
                return;
            }

            EnsureTrigger(controller);

            AnimatorStateMachine sm = FindLayer(controller, LayerName);
            if (sm == null)
            {
                Debug.LogError(
                    $"[PickupAnimationSetup] No se encontró la capa '{LayerName}'"
                );
                return;
            }

            RemoveExisting(sm);
            AddState(sm, clip);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[PickupAnimationSetup] Estado '{StateName}' añadido " +
                $"a la capa '{LayerName}' con velocidad {Speed}x y " +
                $"trigger '{TriggerName}'."
            );
        }

        private static AnimationClip LoadClip()
        {
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(ClipAssetPath))
            {
                if (obj is AnimationClip c && !c.name.StartsWith("__"))
                    return c;
            }
            return null;
        }

        private static void EnsureTrigger(AnimatorController ctrl)
        {
            foreach (AnimatorControllerParameter p in ctrl.parameters)
            {
                if (p.name == TriggerName) return;
            }
            ctrl.AddParameter(TriggerName, AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorStateMachine FindLayer(
            AnimatorController ctrl,
            string layerName
        )
        {
            foreach (AnimatorControllerLayer layer in ctrl.layers)
            {
                if (layer.name == layerName)
                    return layer.stateMachine;
            }
            return null;
        }

        private static void RemoveExisting(AnimatorStateMachine sm)
        {
            foreach (ChildAnimatorState cs in sm.states)
            {
                if (cs.state.name == StateName)
                {
                    sm.RemoveState(cs.state);
                    return;
                }
            }
        }

        private static void AddState(AnimatorStateMachine sm, AnimationClip clip)
        {
            AnimatorState state = sm.AddState(StateName, new Vector3(500f, 100f, 0f));
            state.motion = clip;
            state.speed  = Speed;

            // AnyState → TakingItem (sin exit time, no se transiciona a sí mismo)
            AnimatorStateTransition toPickup = sm.AddAnyStateTransition(state);
            toPickup.hasExitTime       = false;
            toPickup.duration          = 0.08f;
            toPickup.canTransitionToSelf = false;
            toPickup.AddCondition(AnimatorConditionMode.If, 0f, TriggerName);

            // TakingItem → Exit al llegar al 90 % del clip
            AnimatorStateTransition toExit = state.AddExitTransition();
            toExit.hasExitTime = true;
            toExit.exitTime    = 0.9f;
            toExit.duration    = 0.1f;
        }
    }
}
