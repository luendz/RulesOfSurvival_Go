using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    public sealed class LobbyDirectBRButton : MonoBehaviour
    {
        private const string LobbySceneName = "08_Lobby";
        private const string BattleRoyaleSceneName = "07_BattleRoyaleTest";
        private const string DefaultMapName = "Ghillie Island";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForLobby()
        {
            if (SceneManager.GetActiveScene().name != LobbySceneName ||
                Object.FindFirstObjectByType<LobbyDirectBRButton>() != null)
            {
                return;
            }

            GameObject host = new GameObject("Lobby Direct BR Button");
            host.AddComponent<LobbyDirectBRButton>().BuildButton();
        }

        private void BuildButton()
        {
            GameObject canvasObject = new GameObject(
                "Direct BR Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject buttonObject = new GameObject(
                "Iniciar BR",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            buttonObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-28f, 212f);
            rect.sizeDelta = new Vector2(250f, 62f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.75f, 0.14f, 0.08f, 0.98f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(StartBattleRoyale);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.82f, 0.76f, 1f);
            colors.pressedColor = new Color(0.82f, 0.62f, 0.58f, 1f);
            button.colors = colors;

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text)
            );
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = "INICIAR BR";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private static void StartBattleRoyale()
        {
            if (!Application.CanStreamedLevelBeLoaded(BattleRoyaleSceneName))
            {
                Debug.LogError(
                    $"La escena '{BattleRoyaleSceneName}' no está incluida en Build Settings."
                );
                return;
            }

            LobbySession.RequestMatch(LobbyMatchMode.Solo, DefaultMapName);
            SceneManager.LoadScene(BattleRoyaleSceneName);
        }
    }
}
