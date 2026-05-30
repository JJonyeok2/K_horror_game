using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class ThreatAtmosphereCue : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private Light[] threatLights;
        [SerializeField] private float cueDurationSeconds = 4f;
        [SerializeField] private float highThreatFogDensity = 0.055f;
        [SerializeField] private float lightPulseMultiplier = 2.25f;
        [SerializeField] private string feedbackMessage = "Shrine warning: lanterns flare";

        private float[] baseLightIntensities;
        private bool[] baseLightEnabled;
        private bool initialized;
        private bool triggeredForCurrentRun;
        private float remainingSeconds;
        private float baseFogDensity;
        private bool baseFogEnabled;

        public bool IsCueActive { get; private set; }
        public int LightCount => threatLights != null ? threatLights.Length : 0;
        public float CueDurationSeconds => cueDurationSeconds;
        public float HighThreatFogDensity => highThreatFogDensity;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (gameLoop == null || gameLoop.State == null || gameLoop.Resentment == null)
            {
                ManualTick(Time.deltaTime, 0, GameMapId.BongoHub);
                return;
            }

            ManualTick(
                Time.deltaTime,
                gameLoop.Resentment.Stage(),
                gameLoop.State.CurrentMap);
        }

        private void OnDisable()
        {
            RestoreAtmosphere();
        }

        private void OnDestroy()
        {
            RestoreAtmosphere();
        }

        public void Configure(GameLoopController loop, Light[] lights)
        {
            gameLoop = loop;
            threatLights = lights ?? new Light[0];
            initialized = false;
            EnsureInitialized();
        }

        public void TriggerHighThreatCue(string message = null)
        {
            EnsureInitialized();
            IsCueActive = true;
            remainingSeconds = Mathf.Max(cueDurationSeconds, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, highThreatFogDensity);
            ApplyLightMultiplier(lightPulseMultiplier);

            if (gameLoop != null)
            {
                gameLoop.ShowFeedback(string.IsNullOrEmpty(message) ? feedbackMessage : message);
            }
        }

        public void ManualTick(float deltaSeconds, int resentmentStage, GameMapId currentMap)
        {
            EnsureInitialized();
            var isMaximumThreat = currentMap == GameMapId.JonggaEstate
                                  && resentmentStage >= ThreatStageProfile.MaxStage;

            if (isMaximumThreat && !triggeredForCurrentRun)
            {
                triggeredForCurrentRun = true;
                TriggerHighThreatCue();
            }

            if (!isMaximumThreat)
            {
                triggeredForCurrentRun = false;
            }

            if (!IsCueActive)
            {
                return;
            }

            remainingSeconds -= Mathf.Max(deltaSeconds, 0f);
            if (remainingSeconds > 0f)
            {
                var pulse = 1f + Mathf.Abs(Mathf.Sin((cueDurationSeconds - remainingSeconds) * 9f)) * (lightPulseMultiplier - 1f);
                ApplyLightMultiplier(pulse);
                RenderSettings.fog = true;
                RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, highThreatFogDensity);
                return;
            }

            RestoreAtmosphere();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            baseFogEnabled = RenderSettings.fog;
            baseFogDensity = RenderSettings.fogDensity;
            if (threatLights == null)
            {
                threatLights = new Light[0];
            }

            baseLightIntensities = new float[threatLights.Length];
            baseLightEnabled = new bool[threatLights.Length];
            for (var i = 0; i < threatLights.Length; i++)
            {
                baseLightIntensities[i] = threatLights[i] != null ? threatLights[i].intensity : 0f;
                baseLightEnabled[i] = threatLights[i] != null && threatLights[i].enabled;
            }

            initialized = true;
        }

        private void ApplyLightMultiplier(float multiplier)
        {
            if (threatLights == null || baseLightIntensities == null)
            {
                return;
            }

            for (var i = 0; i < threatLights.Length; i++)
            {
                if (threatLights[i] == null)
                {
                    continue;
                }

                threatLights[i].enabled = true;
                threatLights[i].intensity = baseLightIntensities[i] * Mathf.Max(multiplier, 1f);
            }
        }

        private void RestoreAtmosphere()
        {
            IsCueActive = false;
            RestoreLights();
            RenderSettings.fog = baseFogEnabled;
            RenderSettings.fogDensity = baseFogDensity;
        }

        private void RestoreLights()
        {
            if (threatLights == null || baseLightIntensities == null || baseLightEnabled == null)
            {
                return;
            }

            for (var i = 0; i < threatLights.Length; i++)
            {
                if (threatLights[i] == null)
                {
                    continue;
                }

                threatLights[i].intensity = baseLightIntensities[i];
                threatLights[i].enabled = baseLightEnabled[i];
            }
        }
    }
}
