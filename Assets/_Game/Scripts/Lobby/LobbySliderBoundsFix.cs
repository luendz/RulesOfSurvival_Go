using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    /// <summary>
    /// Normaliza la jerarquía visual de los sliders generados en tiempo de ejecución
    /// por LobbySettingsController. Mantiene el relleno y el handle siempre dentro
    /// de la pista, incluso en los valores mínimo y máximo.
    /// </summary>
    public sealed class LobbySliderBoundsFix : MonoBehaviour
    {
        private const string LobbySceneName = "08_Lobby";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForLobby()
        {
            if (SceneManager.GetActiveScene().name != LobbySceneName)
            {
                return;
            }

            GameObject fixer = new GameObject("Lobby Slider Bounds Fix");
            DontDestroyOnLoad(fixer);
            fixer.AddComponent<LobbySliderBoundsFix>();
        }

        private IEnumerator Start()
        {
            // LobbySettingsController crea los sliders en Start. Esperamos un frame
            // para garantizar que toda la jerarquía ya exista antes de normalizarla.
            yield return null;

            FixAllSettingsSliders();

            // El objeto solo es necesario durante la construcción del lobby.
            Destroy(gameObject);
        }

        private static void FixAllSettingsSliders()
        {
            Slider[] sliders = FindObjectsByType<Slider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (Slider slider in sliders)
            {
                if (slider == null || !IsSettingsSlider(slider.transform))
                {
                    continue;
                }

                FixSlider(slider);
            }
        }

        private static bool IsSettingsSlider(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == "Menu Settings")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void FixSlider(Slider slider)
        {
            RectTransform sliderRect = slider.transform as RectTransform;
            if (sliderRect == null)
            {
                return;
            }

            RectTransform background = FindRect(slider.transform, "Background");
            RectTransform fillArea = FindRect(slider.transform, "Fill Area");
            RectTransform handleArea = FindRect(slider.transform, "Handle Slide Area");
            RectTransform fill = fillArea != null ? FindRect(fillArea, "Fill") : null;
            RectTransform handle = handleArea != null ? FindRect(handleArea, "Handle") : null;

            // La pista ocupa todo el ancho del slider y queda centrada verticalmente.
            if (background != null)
            {
                background.anchorMin = new Vector2(0f, 0.5f);
                background.anchorMax = new Vector2(1f, 0.5f);
                background.pivot = new Vector2(0.5f, 0.5f);
                background.anchoredPosition = Vector2.zero;
                background.sizeDelta = new Vector2(0f, 6f);
            }

            // El relleno tiene margen a ambos lados. Además se recorta físicamente
            // para que nunca pueda dibujarse fuera de su pista.
            if (fillArea != null)
            {
                fillArea.anchorMin = new Vector2(0f, 0.5f);
                fillArea.anchorMax = new Vector2(1f, 0.5f);
                fillArea.pivot = new Vector2(0.5f, 0.5f);
                fillArea.offsetMin = new Vector2(8f, -3f);
                fillArea.offsetMax = new Vector2(-8f, 3f);

                if (fillArea.GetComponent<RectMask2D>() == null)
                {
                    fillArea.gameObject.AddComponent<RectMask2D>();
                }
            }

            if (fill != null)
            {
                fill.anchorMin = new Vector2(0f, 0f);
                fill.anchorMax = new Vector2(1f, 1f);
                fill.pivot = new Vector2(0.5f, 0.5f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
            }

            // El handle dispone de margen equivalente a la mitad de su ancho,
            // evitando que sobresalga al alcanzar 0% o 100%.
            if (handleArea != null)
            {
                handleArea.anchorMin = Vector2.zero;
                handleArea.anchorMax = Vector2.one;
                handleArea.pivot = new Vector2(0.5f, 0.5f);
                handleArea.offsetMin = new Vector2(10f, 0f);
                handleArea.offsetMax = new Vector2(-10f, 0f);
            }

            if (handle != null)
            {
                handle.pivot = new Vector2(0.5f, 0.5f);
                handle.sizeDelta = new Vector2(16f, 22f);
            }

            slider.fillRect = fill;
            slider.handleRect = handle;

            // Fuerza a Slider a recalcular las posiciones con la nueva geometría.
            float currentValue = slider.value;
            slider.SetValueWithoutNotify(slider.minValue);
            slider.SetValueWithoutNotify(currentValue);
            LayoutRebuilder.ForceRebuildLayoutImmediate(sliderRect);
        }

        private static RectTransform FindRect(Transform parent, string objectName)
        {
            Transform child = parent.Find(objectName);
            return child as RectTransform;
        }
    }
}
