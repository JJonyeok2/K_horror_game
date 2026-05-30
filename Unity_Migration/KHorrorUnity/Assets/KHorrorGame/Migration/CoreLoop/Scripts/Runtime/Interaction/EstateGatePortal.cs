using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class EstateGatePortal : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform insideSpawn;
        [SerializeField] private Transform outsideSpawn;
        [SerializeField] private float gatePlaneZ = 54f;
        [SerializeField] private float cooldownSeconds = 0.45f;
        [SerializeField] private string enterLabel = "Enter estate [E]";
        [SerializeField] private string exitLabel = "Leave estate [E]";
        [SerializeField] private KoreanHorrorAudioCueBus audioCueBus;

        private float nextAllowedTime;
        private string currentLabel = "Use gate [E]";

        public string InteractionLabel => currentLabel;

        public bool CanInteract(UnityPlayerController actor)
        {
            if (actor == null || Time.time < nextAllowedTime)
            {
                return false;
            }

            currentLabel = IsOutside(actor) ? enterLabel : exitLabel;
            return ResolveTarget(actor) != null;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (!CanInteract(actor))
            {
                return;
            }

            var target = ResolveTarget(actor);
            if (target == null)
            {
                return;
            }

            actor.Teleport(target.position, target.rotation);
            nextAllowedTime = Time.time + cooldownSeconds;
            RequestAudioCue(KoreanHorrorAudioCueBus.GateTransition);
        }

        private bool IsOutside(UnityPlayerController actor)
        {
            return actor.transform.position.z < gatePlaneZ;
        }

        private Transform ResolveTarget(UnityPlayerController actor)
        {
            return IsOutside(actor) ? insideSpawn : outsideSpawn;
        }

        private void RequestAudioCue(string cueKey)
        {
            if (audioCueBus == null)
            {
                audioCueBus = FindObjectOfType<KoreanHorrorAudioCueBus>();
            }

            if (audioCueBus != null)
            {
                audioCueBus.RequestCue(cueKey);
            }
        }
    }
}
