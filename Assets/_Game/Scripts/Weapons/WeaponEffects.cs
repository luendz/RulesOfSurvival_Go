using System.Collections;
using UnityEngine;

namespace ROS.Game.Weapons
{
    public sealed class WeaponEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponController weapon;
        [SerializeField] private Transform muzzle;

        [Header("Muzzle Flash")]
        [Tooltip("Assign a real muzzle-flash ParticleSystem here. No runtime square particle is generated when this is empty.")]
        [SerializeField] private ParticleSystem muzzleFlash;

        [Header("Tracer")]
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private float tracerDuration = 0.04f;

        [Header("Impact")]
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private float impactLifetime = 2f;
        [SerializeField] private float impactSurfaceOffset = 0.01f;

        [Header("Bullet Hole")]
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private float bulletHoleLifetime = 20f;
        [SerializeField] private float bulletHoleSurfaceOffset = 0.002f;
        [SerializeField] private bool randomizeBulletHoleRotation = true;

        private Coroutine _tracerRoutine;
        private Material _runtimeTracerMaterial;

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

            if (muzzle == null)
                muzzle = FindChildRecursive(transform, "MuzzlePoint");

            if (impactPrefab == null)
                impactPrefab = Resources.Load<GameObject>("Effects/PF_BulletImpact");

            if (bulletHolePrefab == null)
                bulletHolePrefab = Resources.Load<GameObject>("Effects/PF_BulletHole");

            // Do not generate a ParticleSystem automatically. The previous runtime
            // fallback rendered as a magenta square on some pipelines. A real muzzle
            // flash can be assigned later without affecting firing/tracer logic.
            EnsureTracer();

            if (tracer != null)
                tracer.enabled = false;
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

        private void OnDestroy()
        {
            if (_runtimeTracerMaterial != null)
                Destroy(_runtimeTracerMaterial);
        }

        public void PlayShot(Vector3 hitPoint, Vector3 hitNormal, bool hasHit)
        {
            PlayMuzzleFlash();
            PlayTracer(hitPoint);

            if (!hasHit)
                return;

            SpawnImpact(hitPoint, hitNormal);
            SpawnBulletHole(hitPoint, hitNormal);
        }

        private void EnsureTracer()
        {
            if (tracer != null)
                return;

            Transform existing = transform.Find("RuntimeTracer");
            GameObject tracerObject;

            if (existing != null)
            {
                tracerObject = existing.gameObject;
                tracer = tracerObject.GetComponent<LineRenderer>();
            }
            else
            {
                tracerObject = new GameObject("RuntimeTracer");
                tracerObject.transform.SetParent(transform, false);
                tracer = tracerObject.AddComponent<LineRenderer>();
            }

            if (tracer == null)
                return;

            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = 0.012f;
            tracer.endWidth = 0.002f;
            tracer.numCapVertices = 2;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.receiveShadows = false;

            if (tracer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    _runtimeTracerMaterial = new Material(shader)
                    {
                        name = "RuntimeTracerMaterial"
                    };

                    if (_runtimeTracerMaterial.HasProperty("_BaseColor"))
                        _runtimeTracerMaterial.SetColor("_BaseColor", new Color(1f, 0.85f, 0.35f, 1f));
                    if (_runtimeTracerMaterial.HasProperty("_Color"))
                        _runtimeTracerMaterial.SetColor("_Color", new Color(1f, 0.85f, 0.35f, 1f));

                    tracer.material = _runtimeTracerMaterial;
                }
            }
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
            tracer.positionCount = 2;
            tracer.SetPosition(0, muzzle.position);
            tracer.SetPosition(1, hitPoint);

            yield return new WaitForSeconds(tracerDuration);

            tracer.enabled = false;
            _tracerRoutine = null;
        }

        private void SpawnImpact(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (impactPrefab == null)
                return;

            Vector3 normal = GetSafeNormal(hitNormal);
            Vector3 spawnPosition = hitPoint + normal * impactSurfaceOffset;
            Quaternion spawnRotation = Quaternion.LookRotation(normal, Vector3.up);

            GameObject impact = Instantiate(impactPrefab, spawnPosition, spawnRotation);
            Destroy(impact, impactLifetime);
        }

        private void SpawnBulletHole(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (bulletHolePrefab == null)
                return;

            Vector3 normal = GetSafeNormal(hitNormal);
            Vector3 spawnPosition = hitPoint + normal * bulletHoleSurfaceOffset;
            Quaternion surfaceRotation = Quaternion.LookRotation(normal, Vector3.up);

            if (randomizeBulletHoleRotation)
            {
                surfaceRotation *= Quaternion.AngleAxis(
                    Random.Range(0f, 360f),
                    Vector3.forward
                );
            }

            GameObject bulletHole = Instantiate(
                bulletHolePrefab,
                spawnPosition,
                surfaceRotation
            );

            Destroy(bulletHole, bulletHoleLifetime);
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
