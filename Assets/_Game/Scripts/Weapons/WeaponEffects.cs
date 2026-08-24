using System;
using System.Collections;
using ROS.Game.Audio;
using ROS.Game.Core;
using ROS.Game.Effects;
using UnityEngine;

namespace ROS.Game.Weapons
{
    public sealed class WeaponEffects : MonoBehaviour
    {
        private const string TracerMaterialResource = "EditorFirst/WeaponTracer";

        [Serializable]
        private sealed class SurfaceImpactVariant
        {
            public ImpactSurfaceType surfaceType = ImpactSurfaceType.Default;
            public GameObject impactPrefab;
            public GameObject bulletHolePrefab;
            public AudioClip[] impactClips;
        }

        [Header("References")]
        [SerializeField] private WeaponController weapon;
        [SerializeField] private Transform muzzle;

        [Header("Muzzle Flash")]
        [Tooltip("ParticleSystem fisico del prefab del arma.")]
        [SerializeField] private ParticleSystem muzzleFlash;

        [Header("Tracer - Editable")]
        [Tooltip("LineRenderer fisico del prefab. No se crea en runtime.")]
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Material tracerMaterial;
        [SerializeField] private float tracerDuration = 0.04f;
        [Range(2, 32)]
        [SerializeField] private int tracerSegments = 10;

        [Header("Impact")]
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private GameObject bloodImpactPrefab;
        [SerializeField] private float impactLifetime = 2f;
        [SerializeField] private float impactSurfaceOffset = 0.01f;

        [Header("Surface Impact Variants")]
        [SerializeField] private SurfaceImpactVariant[] surfaceVariants;
        [SerializeField] private float surfaceProbeRadius = 0.08f;

        [Header("Impact Audio")]
        [SerializeField] private AudioSource impactAudioSource;
        [SerializeField] private AudioClip[] surfaceImpactClips;
        [SerializeField] private AudioClip[] characterImpactClips;

        [Header("Bullet Hole")]
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private float bulletHoleLifetime = 20f;
        [SerializeField] private float bulletHoleSurfaceOffset = 0.002f;
        [SerializeField] private bool randomizeBulletHoleRotation = true;

        private Coroutine _tracerRoutine;
        private WeaponDefinition _definition;
        private float _impactScale = 1f;
        private float _bulletHoleScale = 1f;
        private float _tracerWidth = 0.012f;

        private void Awake()
        {
            EnsureRuntimeSetup();
        }

        private void OnEnable()
        {
            EnsureRuntimeSetup();
        }

        public void EnsureRuntimeSetup()
        {
            if (weapon == null)
                weapon = GetComponent<WeaponController>();

            if (_definition == null && weapon != null)
                _definition = weapon.Definition;

            if (muzzle == null)
                muzzle = FindChildRecursive(transform, "MuzzlePoint");

            if (impactPrefab == null)
                impactPrefab = Resources.Load<GameObject>("Effects/PF_BulletImpact");

            if (bulletHolePrefab == null)
                bulletHolePrefab = Resources.Load<GameObject>("Effects/PF_BulletHole");

            ResolveEditableTracer();

            if (tracer != null)
                tracer.enabled = false;
        }

        public void ConfigureDefinition(WeaponDefinition definition)
        {
            if (definition == null)
                return;

            _definition = definition;
            _impactScale = Mathf.Max(0.05f, definition.impactScale);
            _bulletHoleScale = Mathf.Max(0.05f, definition.bulletHoleScale);
            _tracerWidth = Mathf.Max(0.001f, definition.tracerWidth);

            if (tracer != null)
            {
                tracer.startWidth = _tracerWidth;
                tracer.endWidth = _tracerWidth * 0.2f;
            }
        }

        private void OnDisable()
        {
            if (_tracerRoutine != null)
            {
                StopCoroutine(_tracerRoutine);
                _tracerRoutine = null;
            }

            if (tracer != null)
                tracer.enabled = false;

            if (muzzleFlash != null)
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void ResolveEditableTracer()
        {
            if (tracer == null)
            {
                Transform existing = FindChildRecursive(transform, "Tracer");
                if (existing == null)
                    existing = FindChildRecursive(transform, "RuntimeTracer");
                if (existing != null)
                    tracer = existing.GetComponent<LineRenderer>();
            }

            if (tracer == null)
                return;

            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = _tracerWidth;
            tracer.endWidth = _tracerWidth * 0.2f;
            tracer.numCapVertices = 2;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.receiveShadows = false;

            if (tracerMaterial == null)
                tracerMaterial = Resources.Load<Material>(TracerMaterialResource);

            if (tracer.sharedMaterial == null && tracerMaterial != null)
                tracer.sharedMaterial = tracerMaterial;
        }

        public void PlayShot(
            Vector3 hitPoint,
            Vector3 hitNormal,
            bool hasHit,
            bool hitCharacter = false,
            bool playWeaponFeedback = true)
        {
            if (playWeaponFeedback)
            {
                PlayMuzzleFlash();
                PlayTracer(hitPoint);
            }

            if (!hasHit)
                return;

            ImpactSurfaceType surfaceType = hitCharacter
                ? ImpactSurfaceType.Flesh
                : ResolveSurfaceType(hitPoint);

            SpawnImpact(hitPoint, hitNormal, hitCharacter, surfaceType);
            PlayImpactSound(hitCharacter, surfaceType);

            if (!hitCharacter)
                SpawnBulletHole(hitPoint, hitNormal, surfaceType);
        }

        private void PlayMuzzleFlash()
        {
            if (muzzleFlash == null)
                return;

            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }

        private void PlayTracer(Vector3 hitPoint)
        {
            if (tracer == null || muzzle == null)
                return;

            if (_tracerRoutine != null)
                StopCoroutine(_tracerRoutine);

            _tracerRoutine = StartCoroutine(TracerRoutine(hitPoint));
        }

        private IEnumerator TracerRoutine(Vector3 hitPoint)
        {
            tracer.enabled = true;

            Vector3 start = muzzle.position;
            bool useArc = _definition != null &&
                          _definition.gravityScale > 0f &&
                          _definition.muzzleVelocity > 1f;

            if (!useArc)
            {
                tracer.positionCount = 2;
                tracer.SetPosition(0, start);
                tracer.SetPosition(1, hitPoint);
            }
            else
            {
                int segments = Mathf.Clamp(tracerSegments, 2, 32);
                tracer.positionCount = segments;

                float distance = Vector3.Distance(start, hitPoint);
                float totalTime = WeaponBallistics.EstimateTravelTime(
                    distance,
                    _definition.muzzleVelocity
                );

                Vector3 initialDirection = WeaponBallistics.ApplyDropToAimDirection(
                    start,
                    hitPoint,
                    _definition.muzzleVelocity,
                    _definition.gravityScale
                );
                Vector3 initialVelocity = initialDirection * _definition.muzzleVelocity;

                for (int i = 0; i < segments; i++)
                {
                    float t = totalTime * i / (segments - 1f);
                    Vector3 position = WeaponBallistics.EvaluatePosition(
                        start,
                        initialVelocity,
                        _definition.gravityScale,
                        t
                    );
                    tracer.SetPosition(i, position);
                }

                tracer.SetPosition(segments - 1, hitPoint);
            }

            yield return new WaitForSeconds(tracerDuration);
            tracer.enabled = false;
            _tracerRoutine = null;
        }

        private void SpawnImpact(
            Vector3 hitPoint,
            Vector3 hitNormal,
            bool hitCharacter,
            ImpactSurfaceType surfaceType)
        {
            SurfaceImpactVariant variant = FindVariant(surfaceType);
            GameObject prefab = hitCharacter && bloodImpactPrefab != null
                ? bloodImpactPrefab
                : variant != null && variant.impactPrefab != null
                    ? variant.impactPrefab
                    : impactPrefab;

            if (prefab == null)
                return;

            Vector3 normal = GetSafeNormal(hitNormal);
            Vector3 spawnPosition = hitPoint + normal * impactSurfaceOffset;
            Quaternion spawnRotation = Quaternion.LookRotation(normal, Vector3.up);

            GameObject impact = Instantiate(prefab, spawnPosition, spawnRotation);
            impact.transform.localScale *= _impactScale;
            Destroy(impact, impactLifetime);
        }

        private void PlayImpactSound(bool hitCharacter, ImpactSurfaceType surfaceType)
        {
            if (impactAudioSource == null)
                return;

            AudioClip[] clips;
            if (hitCharacter)
            {
                clips = characterImpactClips;
            }
            else
            {
                SurfaceImpactVariant variant = FindVariant(surfaceType);
                clips = variant != null &&
                        variant.impactClips != null &&
                        variant.impactClips.Length > 0
                    ? variant.impactClips
                    : surfaceImpactClips;
            }

            RandomAudioPlayer.Play(impactAudioSource, clips);
        }

        private void SpawnBulletHole(
            Vector3 hitPoint,
            Vector3 hitNormal,
            ImpactSurfaceType surfaceType)
        {
            SurfaceImpactVariant variant = FindVariant(surfaceType);
            GameObject prefab = variant != null && variant.bulletHolePrefab != null
                ? variant.bulletHolePrefab
                : bulletHolePrefab;

            if (prefab == null)
                return;

            Vector3 normal = GetSafeNormal(hitNormal);
            Vector3 spawnPosition = hitPoint + normal * bulletHoleSurfaceOffset;
            Quaternion surfaceRotation = Quaternion.LookRotation(normal, Vector3.up);

            if (randomizeBulletHoleRotation)
            {
                surfaceRotation *= Quaternion.AngleAxis(
                    UnityEngine.Random.Range(0f, 360f),
                    Vector3.forward
                );
            }

            GameObject bulletHole = Instantiate(prefab, spawnPosition, surfaceRotation);
            bulletHole.transform.localScale *= _bulletHoleScale;
            Destroy(bulletHole, bulletHoleLifetime);
        }

        private ImpactSurfaceType ResolveSurfaceType(Vector3 hitPoint)
        {
            Collider[] colliders = Physics.OverlapSphere(
                hitPoint,
                Mathf.Max(0.001f, surfaceProbeRadius),
                ~0,
                QueryTriggerInteraction.Collide
            );

            foreach (Collider candidate in colliders)
            {
                if (candidate == null)
                    continue;

                ImpactSurface surface = candidate.GetComponentInParent<ImpactSurface>();
                if (surface != null)
                    return surface.SurfaceType;
            }

            return ImpactSurfaceType.Default;
        }

        private SurfaceImpactVariant FindVariant(ImpactSurfaceType surfaceType)
        {
            if (surfaceVariants == null)
                return null;

            foreach (SurfaceImpactVariant variant in surfaceVariants)
            {
                if (variant != null && variant.surfaceType == surfaceType)
                    return variant;
            }

            return null;
        }

        private static Vector3 GetSafeNormal(Vector3 hitNormal)
        {
            return hitNormal.sqrMagnitude <= 0.001f
                ? Vector3.up
                : hitNormal.normalized;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
