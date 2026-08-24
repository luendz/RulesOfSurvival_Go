using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Compatibilidad legacy. El minimapa visual y su camara viven dentro de
    /// HUD_ROS_EDITABLE. RulesOfSurvivalHUD mantiene la camara fisica sobre el
    /// jugador. Este componente conserva Bind sin crear RenderTextures,
    /// Canvas, texturas ni GameObjects en runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapSystem : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private SafeZoneController safeZone;
        [SerializeField] private BattleRoyaleBotDirector botDirector;
        [SerializeField] private Camera minimapCamera;

        public void Bind(
            Transform player,
            SafeZoneController zone,
            BattleRoyaleBotDirector director)
        {
            playerTransform = player;
            safeZone = zone;
            botDirector = director;
            ResolvePhysicalCamera();
        }

        private void Awake()
        {
            ResolvePhysicalCamera();
        }

        private void LateUpdate()
        {
            if (playerTransform == null || minimapCamera == null)
                return;

            Vector3 position = playerTransform.position;
            position.y += 180f;
            minimapCamera.transform.position = position;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void ResolvePhysicalCamera()
        {
            if (minimapCamera != null)
                return;

            RulesOfSurvivalHUD hud = FindFirstObjectByType<RulesOfSurvivalHUD>();
            if (hud == null)
                return;

            Camera[] cameras = hud.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].name != "ROS_MinimapCamera")
                    continue;

                minimapCamera = cameras[i];
                return;
            }
        }
    }
}
