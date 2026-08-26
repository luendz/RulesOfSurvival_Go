using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.World
{
    /// <summary>
    /// Materializa los sistemas jugables de la escena de prueba Battle Royale
    /// dentro de Echo Valley en tiempo de ejecución, manteniendo el mapa y la
    /// iluminación propios de Echo Valley.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EchoValleyBattleRoyaleBootstrap : MonoBehaviour
    {
        [Header("Battle Royale Source")]
        [SerializeField] private string battleRoyaleSceneName = "07_BattleRoyaleTest";

        [Header("Test Environment Roots")]
        [Tooltip("Raíces exclusivas de la arena de prueba que no deben copiarse a Echo Valley.")]
        [SerializeField] private string[] excludedRootNames =
        {
            "Ground",
            "Muros",
            "Directional Light"
        };

        [Header("Lifecycle")]
        [SerializeField] private bool unloadSourceScene = true;

        private bool _isLoading;

        private IEnumerator Start()
        {
            if (_isLoading)
            {
                yield break;
            }

            Scene targetScene = gameObject.scene;
            if (!targetScene.IsValid() || targetScene.name == battleRoyaleSceneName)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(battleRoyaleSceneName))
            {
                Debug.LogError("[EchoValley] No se configuró la escena fuente de Battle Royale.", this);
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(battleRoyaleSceneName))
            {
                Debug.LogError(
                    $"[EchoValley] La escena '{battleRoyaleSceneName}' no está disponible en Build Settings.",
                    this);
                yield break;
            }

            _isLoading = true;

            Scene sourceScene = SceneManager.GetSceneByName(battleRoyaleSceneName);
            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                    battleRoyaleSceneName,
                    LoadSceneMode.Additive);

                if (loadOperation == null)
                {
                    Debug.LogError(
                        $"[EchoValley] No se pudo iniciar la carga de '{battleRoyaleSceneName}'.",
                        this);
                    _isLoading = false;
                    yield break;
                }

                yield return loadOperation;
                sourceScene = SceneManager.GetSceneByName(battleRoyaleSceneName);
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                Debug.LogError(
                    $"[EchoValley] La escena '{battleRoyaleSceneName}' no terminó de cargar correctamente.",
                    this);
                _isLoading = false;
                yield break;
            }

            GameObject[] sourceRoots = sourceScene.GetRootGameObjects();
            int movedRoots = 0;

            foreach (GameObject root in sourceRoots)
            {
                if (root == null || IsExcluded(root.name))
                {
                    continue;
                }

                // Awake de algún sistema puede mover la raíz a DontDestroyOnLoad.
                // En ese caso ya no pertenece a la escena fuente y no debemos moverla otra vez.
                if (root.scene != sourceScene)
                {
                    continue;
                }

                SceneManager.MoveGameObjectToScene(root, targetScene);
                movedRoots++;
            }

            SceneManager.SetActiveScene(targetScene);

            if (unloadSourceScene && sourceScene.IsValid() && sourceScene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sourceScene);
                if (unloadOperation != null)
                {
                    yield return unloadOperation;
                }
            }

            Debug.Log(
                $"[EchoValley] Battle Royale integrado: {movedRoots} raíces trasladadas desde '{battleRoyaleSceneName}'.",
                this);

            _isLoading = false;
        }

        private bool IsExcluded(string rootName)
        {
            if (excludedRootNames == null)
            {
                return false;
            }

            foreach (string excludedName in excludedRootNames)
            {
                if (!string.IsNullOrWhiteSpace(excludedName) &&
                    string.Equals(rootName, excludedName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
