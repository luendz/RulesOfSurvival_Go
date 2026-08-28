using UnityEngine;

namespace ROS.Game.Parachute
{
    public sealed class ParachuteDemoStarter : MonoBehaviour
    {
        [SerializeField] private ParachuteController parachute;
        private void Start()
        {
            if (parachute == null) parachute = GetComponent<ParachuteController>();
            if (parachute != null) parachute.BeginAirDrop();
        }
    }
}
