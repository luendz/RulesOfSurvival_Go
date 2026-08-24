using ROS.Game.Loot;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Compatibilidad legacy. El panel visual de loot de muerte vive dentro de
    /// HUD_ROS_EDITABLE como DeathLootPanelROS y es controlado por
    /// RulesOfSurvivalHUDNearbyLootPresenter. Este componente no crea UI,
    /// GameObjects ni usa OnGUI.
    /// </summary>
    public sealed class DeathLootPanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject boundInteractor;
        [SerializeField] private DeathLootContainer container;

        public bool IsOpen => container != null;
        public DeathLootContainer OpenedContainer => container;

        public void Bind(GameObject interactor)
        {
            boundInteractor = interactor;
        }

        public static DeathLootPanelPresenter OpenOrCreate(
            DeathLootContainer targetContainer,
            GameObject interactor
        )
        {
            DeathLootPanelPresenter presenter =
                FindFirstObjectByType<DeathLootPanelPresenter>();

            if (presenter == null)
            {
                Debug.LogWarning(
                    "[Editor First] Falta DeathLootPanelPresenter fisico en la escena. " +
                    "No se creara uno en runtime."
                );
                return null;
            }

            presenter.Open(targetContainer, interactor);
            return presenter;
        }

        public bool Open(
            DeathLootContainer targetContainer,
            GameObject interactor
        )
        {
            if (targetContainer == null || interactor == null)
                return false;

            container = targetContainer;
            boundInteractor = interactor;

            RulesOfSurvivalHUDNearbyLootPresenter physicalPresenter =
                FindFirstObjectByType<RulesOfSurvivalHUDNearbyLootPresenter>();

            if (physicalPresenter == null)
            {
                Debug.LogWarning(
                    "[Editor First] Falta RulesOfSurvivalHUDNearbyLootPresenter fisico."
                );
                return false;
            }

            return physicalPresenter.Open(targetContainer, interactor);
        }

        public void Close()
        {
            container = null;

            RulesOfSurvivalHUDNearbyLootPresenter physicalPresenter =
                FindFirstObjectByType<RulesOfSurvivalHUDNearbyLootPresenter>();

            if (physicalPresenter != null)
                physicalPresenter.Close();
        }

        private void OnDisable()
        {
            Close();
        }
    }
}
