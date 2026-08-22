using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Lobby
{
    /// <summary>
    /// Elimina la plataforma visual creada por el bootstrap del lobby.
    /// Se mantiene separado para no alterar el resto del entorno, cámara o personaje.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class LobbyStageRemover : MonoBehaviour
    {
        private const string LobbySceneName = "08_Lobby";
        private const string StageObjectName = "Lobby Character Stage";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != LobbySceneName)
            {
                return;
            }

            GameObject helper = new GameObject(nameof(LobbyStageRemover));
            helper.hideFlags = HideFlags.HideInHierarchy;
            helper.AddComponent<LobbyStageRemover>();
        }

        private void Start()
        {
            RemoveStage();
        }

        private void LateUpdate()
        {
            // Respaldo por si el bootstrap crea la plataforma después de Start.
            RemoveStage();
        }

        private void RemoveStage()
        {
            GameObject stage = GameObject.Find(StageObjectName);
            if (stage == null)
            {
                Destroy(gameObject);
                return;
            }

            stage.SetActive(false);
            Destroy(stage);
            Destroy(gameObject);
        }
    }
}
