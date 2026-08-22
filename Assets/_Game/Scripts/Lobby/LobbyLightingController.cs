using UnityEngine;
using UnityEngine.Rendering;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class LobbyLightingController : MonoBehaviour
    {
        [Header("Ambient")]
        [SerializeField] private Color ambientSky = new Color(0.42f, 0.56f, 0.75f, 1f);
        [SerializeField] private Color ambientEquator = new Color(0.28f, 0.36f, 0.48f, 1f);
        [SerializeField] private Color ambientGround = new Color(0.16f, 0.20f, 0.27f, 1f);
        [SerializeField, Range(0f, 2f)] private float ambientIntensity = 1.15f;

        [Header("Key light")]
        [SerializeField] private Color keyColor = new Color(1f, 0.93f, 0.82f, 1f);
        [SerializeField, Range(0f, 4f)] private float keyIntensity = 1.55f;

        [Header("Fill lights")]
        [SerializeField] private Color faceFillColor = new Color(1f, 0.88f, 0.76f, 1f);
        [SerializeField, Range(0f, 8f)] private float faceFillIntensity = 3.6f;
        [SerializeField] private Color coolFillColor = new Color(0.48f, 0.68f, 1f, 1f);
        [SerializeField, Range(0f, 8f)] private float coolFillIntensity = 2.5f;

        [Header("Rim")]
        [SerializeField] private Color rimColor = new Color(1f, 0.62f, 0.32f, 1f);
        [SerializeField, Range(0f, 8f)] private float rimIntensity = 3.4f;

        [Header("Contact shadow")]
        [SerializeField, Range(0f, 0.6f)] private float contactShadowOpacity = 0.28f;
        [SerializeField] private Vector2 contactShadowSize = new Vector2(1.45f, 0.62f);

        private GameObject _lightingRig;
        private Texture2D _shadowTexture;
        private Material _shadowMaterial;

        private void Start()
        {
            ApplyLighting();
        }

        private void OnDestroy()
        {
            if (_shadowMaterial != null)
            {
                Destroy(_shadowMaterial);
            }

            if (_shadowTexture != null)
            {
                Destroy(_shadowTexture);
            }
        }

        [ContextMenu("Apply Lobby Lighting")]
        public void ApplyLighting()
        {
            Camera camera = Camera.main;
            GameObject character = FindLobbyCharacter();

            ConfigureAmbient();

            if (camera != null)
            {
                ConfigureCamera(camera);
            }

            ConfigureBootstrapLights(character);

            if (camera != null && character != null)
            {
                RebuildShowcaseRig(camera, character.transform);
                TuneCharacterRenderers(character);
                CreateContactShadow(character.transform);
            }
        }

        private void ConfigureAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = 0.8f;
            RenderSettings.subtractiveShadowColor = new Color(0.30f, 0.36f, 0.46f, 1f);
        }

        private static void ConfigureCamera(Camera camera)
        {
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.useOcclusionCulling = false;

            if (camera.GetComponent<LobbyColorGradeEffect>() == null)
            {
                camera.gameObject.AddComponent<LobbyColorGradeEffect>();
            }
        }

        private void ConfigureBootstrapLights(GameObject character)
        {
            Light key = FindLight("Lobby Key Light");
            if (key != null)
            {
                key.type = LightType.Directional;
                key.color = keyColor;
                key.intensity = keyIntensity;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.55f;
                key.shadowBias = 0.045f;
                key.shadowNormalBias = 0.35f;
                key.transform.rotation = Quaternion.LookRotation(
                    new Vector3(0.25f, -0.72f, -0.64f),
                    Vector3.up
                );
            }

            Light fill = FindLight("Lobby Fill Light");
            if (fill != null)
            {
                fill.type = LightType.Point;
                fill.color = coolFillColor;
                fill.intensity = coolFillIntensity;
                fill.range = 7.5f;
                fill.shadows = LightShadows.None;

                if (character != null)
                {
                    fill.transform.position =
                        character.transform.position + new Vector3(-2.0f, 2.1f, 2.5f);
                }
            }

            Light rim = FindLight("Lobby Rim Light");
            if (rim != null)
            {
                rim.type = LightType.Point;
                rim.color = rimColor;
                rim.intensity = rimIntensity;
                rim.range = 6.5f;
                rim.shadows = LightShadows.None;

                if (character != null)
                {
                    rim.transform.position =
                        character.transform.position + new Vector3(1.45f, 2.25f, -1.75f);
                }
            }
        }

        private void RebuildShowcaseRig(Camera camera, Transform character)
        {
            GameObject existing = GameObject.Find("Lobby Showcase Lighting Rig");
            if (existing != null)
            {
                Destroy(existing);
            }

            _lightingRig = new GameObject("Lobby Showcase Lighting Rig");
            _lightingRig.transform.SetParent(transform, false);

            Vector3 chestTarget = character.position + Vector3.up * 1.25f;
            Vector3 headTarget = character.position + Vector3.up * 1.62f;

            Vector3 facePosition =
                camera.transform.position
                + camera.transform.right * 0.45f
                + camera.transform.up * 0.35f
                + camera.transform.forward * 0.30f;

            CreateSpotLight(
                "Lobby Face Fill",
                facePosition,
                headTarget,
                faceFillColor,
                faceFillIntensity,
                9f,
                82f,
                LightShadows.None
            );

            Vector3 bodyFillPosition =
                camera.transform.position
                - camera.transform.right * 1.65f
                + camera.transform.up * 0.15f
                + camera.transform.forward * 0.55f;

            CreateSpotLight(
                "Lobby Soft Body Fill",
                bodyFillPosition,
                chestTarget,
                coolFillColor,
                Mathf.Max(0f, coolFillIntensity * 0.75f),
                9f,
                95f,
                LightShadows.None
            );

            Vector3 shoulderRimPosition =
                character.position + new Vector3(-1.15f, 2.15f, -2.1f);

            CreateSpotLight(
                "Lobby Shoulder Rim",
                shoulderRimPosition,
                headTarget,
                new Color(1f, 0.78f, 0.52f, 1f),
                Mathf.Max(0f, rimIntensity * 0.70f),
                6f,
                62f,
                LightShadows.None
            );
        }

        private Light CreateSpotLight(
            string objectName,
            Vector3 position,
            Vector3 target,
            Color color,
            float intensity,
            float range,
            float spotAngle,
            LightShadows shadows
        )
        {
            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(_lightingRig.transform, false);
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.LookRotation(
                (target - position).normalized,
                Vector3.up
            );

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = shadows;
            light.renderMode = LightRenderMode.ForcePixel;

            return light;
        }

        private static void TuneCharacterRenderers(GameObject character)
        {
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;

                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    skinnedRenderer.updateWhenOffscreen = true;
                }
            }
        }

        private void CreateContactShadow(Transform character)
        {
            GameObject oldShadow = GameObject.Find("Lobby Contact Shadow");
            if (oldShadow != null)
            {
                Destroy(oldShadow);
            }

            Shader transparentShader = Shader.Find("Unlit/Transparent");
            if (transparentShader == null)
            {
                return;
            }

            _shadowTexture = BuildRadialShadowTexture(128);
            _shadowMaterial = new Material(transparentShader)
            {
                name = "Lobby Contact Shadow Material",
                color = Color.white,
                mainTexture = _shadowTexture,
                renderQueue = 3000
            };

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "Lobby Contact Shadow";
            shadow.transform.position = character.position + new Vector3(0f, 0.012f, 0.08f);
            shadow.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(
                Mathf.Max(0.1f, contactShadowSize.x),
                Mathf.Max(0.1f, contactShadowSize.y),
                1f
            );

            Collider collider = shadow.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer meshRenderer = shadow.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _shadowMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private Texture2D BuildRadialShadowTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Lobby Contact Shadow Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * contactShadowOpacity;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Light FindLight(string objectName)
        {
            GameObject lightObject = GameObject.Find(objectName);
            return lightObject != null ? lightObject.GetComponent<Light>() : null;
        }

        private static GameObject FindLobbyCharacter()
        {
            GameObject character = GameObject.Find("Lobby Character");
            if (character != null)
            {
                return character;
            }

            return GameObject.Find("Lobby Character Placeholder");
        }
    }
}
