using UnityEngine;
using UnityEngine.Rendering;

namespace KHorrorGame.Migration.Rendering
{
    [CreateAssetMenu(
        fileName = "KHorrorLightingProfile",
        menuName = "K Horror Migration/Rendering/Lighting Profile")]
    public sealed class KHorrorLightingProfile : ScriptableObject
    {
        [Header("Global Fog")]
        [SerializeField] private bool fogEnabled = true;
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [SerializeField, Range(0f, 0.12f)] private float fogDensity = 0.045f;
        [SerializeField] private Color fogColor = new Color(0.025f, 0.032f, 0.028f, 1f);

        [Header("Ambient")]
        [SerializeField] private AmbientMode ambientMode = AmbientMode.Flat;
        [SerializeField] private Color ambientColor = new Color(0.012f, 0.015f, 0.014f, 1f);
        [SerializeField, Range(0f, 1f)] private float reflectionIntensity = 0.08f;

        [Header("Moon")]
        [SerializeField] private Color moonColor = new Color(0.45f, 0.56f, 0.68f, 1f);
        [SerializeField, Range(0f, 2f)] private float moonIntensity = 0.14f;
        [SerializeField] private Vector3 moonEulerAngles = new Vector3(52f, -35f, 0f);

        [Header("Player Flashlight")]
        [SerializeField] private Color flashlightColor = new Color(0.82f, 0.9f, 0.78f, 1f);
        [SerializeField, Range(0f, 20f)] private float flashlightIntensity = 5.5f;
        [SerializeField, Range(1f, 30f)] private float flashlightRange = 15f;
        [SerializeField, Range(1f, 90f)] private float flashlightSpotAngle = 42f;

        [Header("Post Exposure Reference")]
        [SerializeField, Range(-5f, 5f)] private float exposureCompensation = -1.2f;
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.22f;
        [SerializeField, Range(-100f, 100f)] private float saturation = -18f;

        public bool FogEnabled => fogEnabled;
        public FogMode FogMode => fogMode;
        public float FogDensity => fogDensity;
        public Color FogColor => fogColor;
        public AmbientMode AmbientMode => ambientMode;
        public Color AmbientColor => ambientColor;
        public float ReflectionIntensity => reflectionIntensity;
        public Color MoonColor => moonColor;
        public float MoonIntensity => moonIntensity;
        public Vector3 MoonEulerAngles => moonEulerAngles;
        public Color FlashlightColor => flashlightColor;
        public float FlashlightIntensity => flashlightIntensity;
        public float FlashlightRange => flashlightRange;
        public float FlashlightSpotAngle => flashlightSpotAngle;
        public float ExposureCompensation => exposureCompensation;
        public float VignetteIntensity => vignetteIntensity;
        public float Saturation => saturation;
    }
}
