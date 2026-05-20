using UnityEngine;

namespace KHorrorGame.Migration.Rendering
{
    [ExecuteAlways]
    public sealed class KHorrorLightingRig : MonoBehaviour
    {
        [SerializeField] private KHorrorLightingProfile profile;
        [SerializeField] private Light moonLight;
        [SerializeField] private Light playerFlashlight;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool applyInEditor = true;

        private void Awake()
        {
            if (applyOnAwake)
            {
                ApplyProfile();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && applyInEditor)
            {
                ApplyProfile();
            }
        }
#endif

        public void ApplyProfile()
        {
            if (profile == null)
            {
                return;
            }

            RenderSettings.fog = profile.FogEnabled;
            RenderSettings.fogMode = profile.FogMode;
            RenderSettings.fogDensity = profile.FogDensity;
            RenderSettings.fogColor = profile.FogColor;
            RenderSettings.ambientMode = profile.AmbientMode;
            RenderSettings.ambientLight = profile.AmbientColor;
            RenderSettings.reflectionIntensity = profile.ReflectionIntensity;

            ApplyMoonLight();
            ApplyFlashlight();
            ApplyCameraBackground();
        }

        private void ApplyMoonLight()
        {
            if (moonLight == null)
            {
                return;
            }

            moonLight.type = LightType.Directional;
            moonLight.color = profile.MoonColor;
            moonLight.intensity = profile.MoonIntensity;
            moonLight.transform.rotation = Quaternion.Euler(profile.MoonEulerAngles);
        }

        private void ApplyFlashlight()
        {
            if (playerFlashlight == null)
            {
                return;
            }

            playerFlashlight.type = LightType.Spot;
            playerFlashlight.color = profile.FlashlightColor;
            playerFlashlight.intensity = profile.FlashlightIntensity;
            playerFlashlight.range = profile.FlashlightRange;
            playerFlashlight.spotAngle = profile.FlashlightSpotAngle;
        }

        private void ApplyCameraBackground()
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = profile.FogColor;
        }
    }
}
