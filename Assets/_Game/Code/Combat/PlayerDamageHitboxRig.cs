using UnityEngine;

namespace ROS.Game.Combat
{
    /// <summary>
    /// Contrato del rig físico de daño. Los hitboxes deben existir en el
    /// prefab; este componente nunca los genera durante Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDamageHitboxRig : MonoBehaviour
    {
        private const string RigName = "RuntimeDamageHitboxes";

        public bool HasGeneratedHitboxes => transform.Find(RigName) != null;

        private void Awake()
        {
            Transform rig = transform.Find(RigName);
            DamageHitbox[] hitboxes = rig != null
                ? rig.GetComponentsInChildren<DamageHitbox>(true)
                : System.Array.Empty<DamageHitbox>();

            if (rig == null || hitboxes.Length < 6)
            {
                Debug.LogError(
                    $"[{nameof(PlayerDamageHitboxRig)}] El prefab '{name}' no contiene el rig físico completo.",
                    this);
                enabled = false;
            }
        }
    }
}
