using ROS.Game.Interaction;
using UnityEngine;

namespace ROS.Game.World
{
    /// <summary>
    /// Puerta interactiva. Rota 90° al activar con F/E via PlayerInteractor.
    /// El pivot debe estar en el borde-bisagra (no en el centro del panel).
    /// </summary>
    public sealed class DoorController : MonoBehaviour, IInteractable
    {
        private const float OpenAngle  = 90f;
        private const float LerpSpeed  = 8f;

        private bool  _isOpen;
        private float _targetAngle;   // local Y
        private float _currentAngle;

        // ----------------------------------------------------------------
        // IInteractable

        public string InteractionLabel =>
            _isOpen ? "Cerrar puerta" : "Abrir puerta";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            _isOpen      = !_isOpen;
            _targetAngle = _isOpen ? OpenAngle : 0f;
        }

        // ----------------------------------------------------------------

        private void Awake()
        {
            _currentAngle = transform.localEulerAngles.y;
            _targetAngle  = _currentAngle;
        }

        private void Update()
        {
            _currentAngle = Mathf.LerpAngle(
                _currentAngle,
                _targetAngle,
                Time.deltaTime * LerpSpeed
            );
            transform.localEulerAngles = new Vector3(0f, _currentAngle, 0f);
        }

        // ----------------------------------------------------------------
        // Factory

        /// <summary>
        /// Crea una puerta en el parent dado, con pivot en la bisagra izquierda.
        /// </summary>
        public static DoorController Create(
            Transform parent,
            Vector3   localHingePos,
            float     width,
            float     height,
            float     thickness,
            Material  mat = null)
        {
            // Pivot en la bisagra
            var pivot = new GameObject("Door_Pivot");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localHingePos;

            DoorController door = pivot.AddComponent<DoorController>();

            // Panel físico (bloquea paso cuando cerrado)
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Door_Panel";
            panel.transform.SetParent(pivot.transform, false);
            panel.transform.localPosition = new Vector3(width * 0.5f, 0f, 0f);
            panel.transform.localScale    = new Vector3(width, height, thickness);
            if (mat != null)
                panel.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Trigger para detección de interacción (ligeramente más grande)
            var triggerGo = new GameObject("Door_InteractTrigger");
            triggerGo.transform.SetParent(pivot.transform, false);
            triggerGo.transform.localPosition = new Vector3(width * 0.5f, 0f, 0f);
            var triggerCol = triggerGo.AddComponent<BoxCollider>();
            triggerCol.size      = new Vector3(width + 1.2f, height, thickness + 1.0f);
            triggerCol.isTrigger = true;

            return door;
        }
    }
}
