using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class SettlementThreatZone : MonoBehaviour
    {
        [SerializeField] private int damagePerPulse = 22;
        [SerializeField] private float damageIntervalSeconds = 1.15f;

        private float cooldownSeconds;

        private void OnValidate()
        {
            damagePerPulse = Mathf.Max(1, damagePerPulse);
            damageIntervalSeconds = Mathf.Max(0.1f, damageIntervalSeconds);
        }

        private void OnTriggerStay(Collider other)
        {
            var actor = other != null ? other.GetComponentInParent<UnityPlayerController>() : null;
            ManualTick(actor, Time.deltaTime);
        }

        public bool ManualTick(UnityPlayerController actor, float deltaSeconds)
        {
            if (actor == null)
            {
                return false;
            }

            cooldownSeconds = Mathf.Max(0f, cooldownSeconds - Mathf.Max(0f, deltaSeconds));
            if (cooldownSeconds > 0f)
            {
                return false;
            }

            var receiver = actor.GetComponent<PlayerDamageReceiver>();
            if (receiver == null || !receiver.ApplyDamage(damagePerPulse, EnemyKind.Ghost))
            {
                return false;
            }

            cooldownSeconds = damageIntervalSeconds;
            return true;
        }
    }
}
