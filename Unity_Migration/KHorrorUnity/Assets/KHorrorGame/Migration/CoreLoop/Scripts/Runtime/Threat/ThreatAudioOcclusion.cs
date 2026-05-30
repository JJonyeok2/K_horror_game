using System;
using UnityEngine;

namespace KHorrorGame.Migration
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(AudioLowPassFilter))]
    public sealed class ThreatAudioOcclusion : MonoBehaviour
    {
        [SerializeField] private EnemyBrain sourceEnemy;
        [SerializeField] private Transform listenerTarget;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioLowPassFilter lowPassFilter;
        [SerializeField] private LayerMask occlusionMask = ~0;
        [SerializeField] private float audibleRange = 18f;
        [SerializeField] private float clearCutoffHz = 22000f;
        [SerializeField] private float occludedCutoffHz = 1200f;
        [SerializeField] private float minVolume = 0.08f;
        [SerializeField] private float maxVolume = 1f;
        [SerializeField] private float occludedVolumeMultiplier = 0.32f;

        private Transform listenerOcclusionRoot;
        private static AudioClip estateGhostClip;
        private static AudioClip forestDokkaebiClip;

        public bool IsOccluded { get; private set; }
        public float CurrentVolume { get; private set; }
        public float CurrentCutoffFrequency { get; private set; }
        public string CurrentCueLabel { get; private set; } = string.Empty;

        private void Awake()
        {
            EnsureReferences();
            ApplySourceDefaults();
        }

        private void Update()
        {
            ManualRefresh();
        }

        public void Configure(
            EnemyBrain enemy,
            Transform listener,
            AudioSource source = null,
            AudioLowPassFilter filter = null)
        {
            sourceEnemy = enemy;
            listenerTarget = listener;
            audioSource = source != null ? source : audioSource;
            lowPassFilter = filter != null ? filter : lowPassFilter;
            EnsureReferences();
            ApplySourceDefaults();
        }

        public void ManualRefresh()
        {
            EnsureReferences();
            CurrentCueLabel = ResolveCueLabel();

            if (audioSource == null || lowPassFilter == null || listenerTarget == null)
            {
                return;
            }

            EnsureCueClip(CurrentCueLabel);
            var distance = Vector3.Distance(transform.position, listenerTarget.position);
            var rangeRatio = Mathf.Clamp01(distance / Mathf.Max(audibleRange, 0.001f));
            var baseVolume = Mathf.Lerp(maxVolume, minVolume, rangeRatio);

            IsOccluded = HasOccluderBetween(transform.position, listenerTarget.position);
            CurrentVolume = Mathf.Clamp01(IsOccluded ? baseVolume * occludedVolumeMultiplier : baseVolume);
            CurrentCutoffFrequency = IsOccluded ? occludedCutoffHz : clearCutoffHz;

            audioSource.volume = CurrentVolume;
            lowPassFilter.cutoffFrequency = CurrentCutoffFrequency;
            if (Application.isPlaying && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void EnsureReferences()
        {
            if (sourceEnemy == null)
            {
                sourceEnemy = GetComponent<EnemyBrain>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (lowPassFilter == null)
            {
                lowPassFilter = GetComponent<AudioLowPassFilter>();
            }

            if (listenerTarget == null)
            {
                var listener = FindObjectOfType<AudioListener>();
                if (listener != null)
                {
                    listenerTarget = listener.transform;
                }
            }

            listenerOcclusionRoot = ResolveListenerOcclusionRoot();
        }

        private void ApplySourceDefaults()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.spatialBlend = 1f;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = Mathf.Max(audibleRange, 1.5f);
            EnsureCueClip(ResolveCueLabel());
        }

        private bool HasOccluderBetween(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            var distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return false;
            }

            var hits = Physics.RaycastAll(
                from,
                direction / distance,
                distance,
                occlusionMask,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null || IsOwnCollider(hit.collider) || IsListenerCollider(hit.collider))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool IsOwnCollider(Collider candidate)
        {
            return candidate.transform == transform || candidate.transform.IsChildOf(transform);
        }

        private bool IsListenerCollider(Collider candidate)
        {
            if (listenerTarget == null)
            {
                return false;
            }

            if (candidate.transform == listenerTarget || candidate.transform.IsChildOf(listenerTarget))
            {
                return true;
            }

            return listenerOcclusionRoot != null
                   && (candidate.transform == listenerOcclusionRoot ||
                       candidate.transform.IsChildOf(listenerOcclusionRoot));
        }

        private Transform ResolveListenerOcclusionRoot()
        {
            if (listenerTarget == null)
            {
                return null;
            }

            var player = listenerTarget.GetComponentInParent<UnityPlayerController>();
            if (player != null)
            {
                return player.transform;
            }

            var controller = listenerTarget.GetComponentInParent<CharacterController>();
            return controller != null ? controller.transform : listenerTarget;
        }

        private void EnsureCueClip(string cueLabel)
        {
            if (audioSource == null || string.IsNullOrEmpty(cueLabel))
            {
                return;
            }

            var desiredClip = ClipForCue(cueLabel);
            if (audioSource.clip != desiredClip)
            {
                audioSource.clip = desiredClip;
            }
        }

        private string ResolveCueLabel()
        {
            var kind = sourceEnemy != null ? sourceEnemy.EnemyKind : EnemyKind.Ghost;
            var territory = sourceEnemy != null ? sourceEnemy.HomeTerritory : TerritoryKind.EstateInterior;

            if (kind == EnemyKind.Dokkaebi || territory == TerritoryKind.ForestApproach)
            {
                return "forest_dokkaebi_presence";
            }

            return "estate_ghost_presence";
        }

        private static AudioClip ClipForCue(string cueLabel)
        {
            if (cueLabel == "forest_dokkaebi_presence")
            {
                forestDokkaebiClip = forestDokkaebiClip != null
                    ? forestDokkaebiClip
                    : CreateProceduralCue("forest_dokkaebi_presence_generated_loop", 76f, 0.26f);
                return forestDokkaebiClip;
            }

            estateGhostClip = estateGhostClip != null
                ? estateGhostClip
                : CreateProceduralCue("estate_ghost_presence_generated_loop", 41f, 0.18f);
            return estateGhostClip;
        }

        private static AudioClip CreateProceduralCue(string clipName, float frequency, float amplitude)
        {
            const int sampleRate = 22050;
            const int sampleCount = sampleRate;
            var data = new float[sampleCount];
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)sampleRate;
                var baseTone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                var tremble = Mathf.Sin(2f * Mathf.PI * frequency * 0.37f * t) * 0.45f;
                data[i] = (baseTone + tremble) * amplitude;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
