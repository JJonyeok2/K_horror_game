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
        [SerializeField, Range(0f, 0.12f)] private float fogDensity = 0.021f;
        [SerializeField] private Color fogColor = new Color(0.045f, 0.052f, 0.052f, 1f);

        [Header("Ambient")]
        [SerializeField] private AmbientMode ambientMode = AmbientMode.Flat;
        [SerializeField] private Color ambientColor = new Color(0.062f, 0.066f, 0.058f, 1f);
        [SerializeField, Range(0f, 1f)] private float reflectionIntensity = 0.16f;

        [Header("Moon")]
        [SerializeField] private Color moonColor = new Color(0.58f, 0.67f, 0.78f, 1f);
        [SerializeField, Range(0f, 2f)] private float moonIntensity = 0.42f;
        [SerializeField] private Vector3 moonEulerAngles = new Vector3(52f, -35f, 0f);

        [Header("Player Flashlight")]
        [SerializeField] private Color flashlightColor = new Color(0.82f, 0.9f, 0.78f, 1f);
        [SerializeField, Range(0f, 20f)] private float flashlightIntensity = 14f;
        [SerializeField, Range(1f, 40f)] private float flashlightRange = 34f;
        [SerializeField, Range(1f, 90f)] private float flashlightSpotAngle = 54f;

        [Header("Post Exposure Reference")]
        [SerializeField, Range(-5f, 5f)] private float exposureCompensation = 0.35f;
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.16f;
        [SerializeField, Range(-100f, 100f)] private float saturation = -8f;

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
