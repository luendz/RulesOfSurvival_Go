using ROS.Game.Character;
using ROS.Game.Parachute;
using UnityEngine;

namespace ROS.Game.Animation
{
    /// <summary>
    /// Evita entrar en Fall durante saltos o desniveles pequenos.
    /// ShouldFall solo se activa cuando el personaje ya esta descendiendo y
    /// ha caido una distancia minima desde el punto mas alto alcanzado.
    ///
    /// Este componente debe existir de forma serializada en el Player para que
    /// sus valores se vean y se ajusten directamente desde el Inspector.
    /// </summary>
    [AddComponentMenu("Rules Of Survival/Animation/Player Fall Animation Gate")]
    [DefaultExecutionOrder(70)]
    [DisallowMultipleComponent]
    public sealed class PlayerFallAnimationGate : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private ParachuteController parachute;

        [Header("Fall Animation")]
        [Tooltip("Distancia minima, en metros, que debe caer el jugador desde el punto mas alto antes de entrar en Fall.")]
        [SerializeField, Min(0.05f)] private float fallDistanceThreshold = 0.85f;

        [Tooltip("Velocidad vertical maxima para considerar que el jugador ya esta descendiendo.")]
        [SerializeField] private float descendingVelocityThreshold = -0.2f;

        [Header("Runtime Debug")]
        [SerializeField] private float debugPeakY;
        [SerializeField] private float debugFallDistance;
        [SerializeField] private bool debugShouldFall;

        private static readonly int ShouldFall = Animator.StringToHash("ShouldFall");
        private bool _wasGrounded = true;
        private float _peakY;

        private void Awake()
        {
            ResolveReferences();
            ResetTracking();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResetTracking();
        }

        private void Update()
        {
            ResolveReferences();

            if (animator == null || motor == null)
                return;

            bool parachuting = parachute != null && parachute.IsAirbornePhase;
            bool grounded = !parachuting && motor.IsGrounded;
            float currentY = transform.position.y;

            if (grounded)
            {
                _peakY = currentY;
                _wasGrounded = true;
                SetShouldFall(false);
                debugFallDistance = 0f;
                debugPeakY = _peakY;
                debugShouldFall = false;
                return;
            }

            if (_wasGrounded)
            {
                _peakY = currentY;
                _wasGrounded = false;
            }

            if (currentY > _peakY)
                _peakY = currentY;

            float fallDistance = Mathf.Max(0f, _peakY - currentY);
            float verticalVelocity = parachuting && parachute != null
                ? parachute.VerticalSpeed
                : motor.Velocity.y;

            bool shouldFall = !parachuting &&
                              verticalVelocity <= descendingVelocityThreshold &&
                              fallDistance >= fallDistanceThreshold;

            SetShouldFall(shouldFall);

            debugPeakY = _peakY;
            debugFallDistance = fallDistance;
            debugShouldFall = shouldFall;
        }

        private void OnDisable()
        {
            SetShouldFall(false);
        }

        private void ResetTracking()
        {
            _peakY = transform.position.y;
            _wasGrounded = motor == null || motor.IsGrounded;
            debugPeakY = _peakY;
            debugFallDistance = 0f;
            debugShouldFall = false;
            SetShouldFall(false);
        }

        private void SetShouldFall(bool value)
        {
            if (animator == null || !HasShouldFallParameter())
                return;

            if (animator.GetBool(ShouldFall) != value)
                animator.SetBool(ShouldFall, value);
        }

        private bool HasShouldFallParameter()
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.nameHash == ShouldFall &&
                    parameter.type == AnimatorControllerParameterType.Bool)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (motor == null)
                motor = GetComponent<PlayerMotor>() ?? GetComponentInParent<PlayerMotor>();

            if (parachute == null)
                parachute = GetComponent<ParachuteController>() ??
                            GetComponentInParent<ParachuteController>();
        }
    }
}
