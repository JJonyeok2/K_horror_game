using System;
using System.Collections.Generic;
using UnityEngine;

namespace KHorrorGame.Migration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class KoreanHorrorAudioCueBus : MonoBehaviour
    {
        public const string GateTransition = "gate_transition";
        public const string TerminalAccepted = "terminal_accepted";
        public const string TerminalDenied = "terminal_denied";
        public const string CargoLoaded = "cargo_loaded";
        public const string ResentmentStageUp = "resentment_stage_up";
        public const string GhostNearby = "ghost_nearby";
        public const string DokkaebiCue = "forest_dokkaebi_cue";
        public const string ThreatWarningCue = "threat_warning_cue";

        private const int SampleRate = 22050;
        private const int ClipSampleCount = 2205;

        [SerializeField] private AudioSource output;
        [SerializeField] private bool autoGeneratePlaceholderClips = true;
        [SerializeField] private float outputVolume = 0.48f;

        private readonly List<string> cueHistory = new List<string>();
        private readonly Dictionary<string, AudioClip> generatedClips = new Dictionary<string, AudioClip>();

        public IReadOnlyList<string> CueHistory
        {
            get { return cueHistory; }
        }

        public string LastCueKey { get; private set; } = string.Empty;
        public int CueCount
        {
            get { return cueHistory.Count; }
        }

        private void Awake()
        {
            EnsureOutput();
        }

        public bool RequestCue(string cueKey)
        {
            if (string.IsNullOrWhiteSpace(cueKey))
            {
                return false;
            }

            EnsureOutput();
            LastCueKey = cueKey;
            cueHistory.Add(cueKey);

            if (output != null && autoGeneratePlaceholderClips)
            {
                output.clip = ResolvePlaceholderClip(cueKey);
                if (Application.isPlaying && output.enabled && output.gameObject.activeInHierarchy)
                {
                    output.Play();
                }
            }

            return true;
        }

        public void ClearHistory()
        {
            cueHistory.Clear();
            LastCueKey = string.Empty;
        }

        public static bool IsAggressiveThreatCue(string cueKey)
        {
            return string.Equals(cueKey, GhostNearby, StringComparison.Ordinal)
                   || string.Equals(cueKey, DokkaebiCue, StringComparison.Ordinal);
        }

        private void EnsureOutput()
        {
            if (output == null)
            {
                output = GetComponent<AudioSource>();
            }

            if (output == null)
            {
                output = gameObject.AddComponent<AudioSource>();
            }

            output.playOnAwake = false;
            output.loop = false;
            output.spatialBlend = 0f;
            output.volume = Mathf.Clamp01(outputVolume);
        }

        private AudioClip ResolvePlaceholderClip(string cueKey)
        {
            if (generatedClips.TryGetValue(cueKey, out var clip) && clip != null)
            {
                return clip;
            }

            clip = AudioClip.Create("placeholder_" + cueKey, ClipSampleCount, 1, SampleRate, false);
            var samples = new float[ClipSampleCount];
            var frequency = FrequencyForCue(cueKey);
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = Mathf.Clamp01(1f - i / (float)samples.Length);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.18f * envelope;
            }

            clip.SetData(samples, 0);
            generatedClips[cueKey] = clip;
            return clip;
        }

        private static float FrequencyForCue(string cueKey)
        {
            var hash = cueKey == null ? 0 : Mathf.Abs(cueKey.GetHashCode());
            return 140f + hash % 320;
        }
    }
}
