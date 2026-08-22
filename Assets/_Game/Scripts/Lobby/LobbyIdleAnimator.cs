using UnityEngine;

namespace ROS.Game.Lobby
{
    public static class LobbyIdleAnimator
    {
        private const string ControllerResourcePath = "Lobby/AC_LobbyIdle";
        private const string LobbyCharacterName = "Lobby Character";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyLobbyIdle()
        {
            LobbySceneBootstrap lobby =
                Object.FindFirstObjectByType<LobbySceneBootstrap>();

            if (lobby == null)
            {
                return;
            }

            GameObject character = GameObject.Find(LobbyCharacterName);
            if (character == null)
            {
                Debug.LogWarning(
                    "LobbyIdleAnimator no encontró el personaje del lobby."
                );
                return;
            }

            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>(ControllerResourcePath);

            if (controller == null)
            {
                Debug.LogError(
                    $"No se encontró el controlador de animación en Resources/{ControllerResourcePath}."
                );
                return;
            }

            Animator[] animators =
                character.GetComponentsInChildren<Animator>(true);

            if (animators.Length == 0)
            {
                Debug.LogError(
                    "El personaje del lobby no contiene un Animator. " +
                    "Revisa el prefab MainCharacter_Male_01."
                );
                return;
            }

            bool applied = false;

            foreach (Animator animator in animators)
            {
                if (animator == null || animator.avatar == null)
                {
                    continue;
                }

                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                animator.Update(0f);
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);

                applied = true;
            }

            if (!applied)
            {
                Debug.LogError(
                    "Se encontraron Animator en el personaje del lobby, " +
                    "pero ninguno tiene un Avatar válido para reproducir Idle."
                );
            }
        }
    }
}
