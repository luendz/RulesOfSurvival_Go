using System;
using UnityEngine;

namespace ROS.Game.Network
{
    /// <summary>
    /// Boundary used by gameplay code so networking can later be backed by NGO/Entities
    /// without rewriting character, inventory, weapon or match systems.
    /// </summary>
    public sealed class NetworkGameFacade : MonoBehaviour
    {
        public bool IsNetworked { get; private set; }
        public bool IsServerAuthority { get; private set; } = true;
        public event Action<bool> NetworkModeChanged;

        public void ConfigureLocal() { IsNetworked = false; IsServerAuthority = true; NetworkModeChanged?.Invoke(false); }
        public void ConfigureNetworked(bool serverAuthority) { IsNetworked = true; IsServerAuthority = serverAuthority; NetworkModeChanged?.Invoke(true); }
    }
}
