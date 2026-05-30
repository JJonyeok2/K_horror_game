using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class BongoTravelSequenceController : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private AudioSource travelAudio;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Transform motionRig;
        [SerializeField] private float activeFadeAlpha = 0.72f;
        [SerializeField] private float motionAmplitude = 0.06f;
        [SerializeField] private float motionFrequency = 5f;

        private GameLoopController subscribedGameLoop;
        private Vector3 motionRigBaseLocalPosition;
        private float motionTime;

        public bool IsTravelSequenceActive { get; private set; }
        public bool TravelAudioRequested { get; private set; }

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            if (fadeGroup == null)
            {
                fadeGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            if (travelAudio == null)
            {
                travelAudio = GetComponent<AudioSource>();
            }

            if (travelAudio == null)
            {
                travelAudio = gameObject.AddComponent<AudioSource>();
            }

            if (motionRig == null && transform.childCount > 0)
            {
                motionRig = transform.GetChild(0);
            }

            motionRigBaseLocalPosition = motionRig != null ? motionRig.localPosition : Vector3.zero;
            ConfigureAudioSource();
            ManualRefresh();
        }

        private void OnEnable()
        {
            SubscribeToGameLoop(gameLoop);
            ManualRefresh();
        }

        private void OnDisable()
        {
            if (subscribedGameLoop != null)
            {
                subscribedGameLoop.StateChanged -= OnGameLoopChanged;
                subscribedGameLoop = null;
            }

            StopTravelAudio();
        }

        private void Update()
        {
            ManualTick(Time.deltaTime);
        }

        public void ManualRefresh()
        {
            var shouldPlay = gameLoop != null && gameLoop.State != null && gameLoop.State.IsTraveling;
            SetSequenceActive(shouldPlay);
        }

        public void ManualTick(float deltaTime)
        {
            ManualRefresh();

            if (!IsTravelSequenceActive || motionRig == null)
            {
                return;
            }

            motionTime += Mathf.Max(deltaTime, 0f);
            var phase = motionTime * Mathf.Max(motionFrequency, 0.01f);
            motionRig.localPosition = motionRigBaseLocalPosition + new Vector3(
                Mathf.Sin(phase) * motionAmplitude,
                Mathf.Sin(phase * 0.73f) * motionAmplitude * 0.35f,
                0f);
        }

        private void OnGameLoopChanged(GameLoopController controller)
        {
            if (controller != null)
            {
                gameLoop = controller;
            }

            ManualRefresh();
        }

        private void SubscribeToGameLoop(GameLoopController source)
        {
            if (subscribedGameLoop == source)
            {
                return;
            }

            if (subscribedGameLoop != null)
            {
                subscribedGameLoop.StateChanged -= OnGameLoopChanged;
                subscribedGameLoop = null;
            }

            if (!isActiveAndEnabled || source == null)
            {
                return;
            }

            subscribedGameLoop = source;
            subscribedGameLoop.StateChanged += OnGameLoopChanged;
        }

        private void SetSequenceActive(bool active)
        {
            IsTravelSequenceActive = active;

            if (fadeGroup != null)
            {
                fadeGroup.alpha = active ? activeFadeAlpha : 0f;
                fadeGroup.blocksRaycasts = active;
                fadeGroup.interactable = false;
            }

            if (active)
            {
                StartTravelAudio();
                return;
            }

            StopTravelAudio();
            motionTime = 0f;
            if (motionRig != null)
            {
                motionRig.localPosition = motionRigBaseLocalPosition;
            }
        }

        private void ConfigureAudioSource()
        {
            if (travelAudio == null)
            {
                return;
            }

            travelAudio.playOnAwake = false;
            travelAudio.loop = true;
            travelAudio.spatialBlend = 0f;
            travelAudio.volume = 0.18f;
            EnsureTravelClip();
        }

        private void StartTravelAudio()
        {
            TravelAudioRequested = true;
            if (travelAudio == null)
            {
                return;
            }

            EnsureTravelClip();
            if (!travelAudio.isPlaying)
            {
                travelAudio.Play();
            }
        }

        private void StopTravelAudio()
        {
            TravelAudioRequested = false;
            if (travelAudio != null && travelAudio.isPlaying)
            {
                travelAudio.Stop();
            }
        }

        private void EnsureTravelClip()
        {
            if (travelAudio == null || travelAudio.clip != null)
            {
                return;
            }

            const int sampleRate = 22050;
            var samples = new float[sampleRate];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)sampleRate;
                var enginePulse = Mathf.Sin(2f * Mathf.PI * 54f * t) * 0.09f;
                var lowRumble = Mathf.Sin(2f * Mathf.PI * 18f * t) * 0.05f;
                samples[i] = enginePulse + lowRumble;
            }

            var clip = AudioClip.Create("generated_bongo_engine_hum", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            travelAudio.clip = clip;
        }
    }
}
