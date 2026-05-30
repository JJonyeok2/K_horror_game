using UnityEngine;

namespace KHorrorGame.Migration
{
    public abstract class EnemyController : MonoBehaviour
    {
        [SerializeField] protected EnemyBrain brain;
        [SerializeField] protected Transform target;
        [SerializeField] protected TerritoryKind homeTerritory = TerritoryKind.EstateInterior;
        [SerializeField] protected Vector3 homePosition;
        [SerializeField] protected bool automaticTick = true;
        [SerializeField] private float returnArrivalDistance = 0.35f;

        private readonly EnemyTerritoryRules territoryRules = EnemyTerritoryRules.Default;

        public EnemyControllerState ControllerState { get; private set; } = EnemyControllerState.Dormant;
        public float TargetDistance { get; private set; }
        public bool TargetTerritoryAllowed { get; private set; }
        public bool AutomaticTickEnabled
        {
            get { return automaticTick; }
        }

        public EnemyBrain Brain
        {
            get
            {
                if (brain == null)
                {
                    brain = GetComponent<EnemyBrain>();
                }

                return brain;
            }
        }

        public TerritoryKind HomeTerritory
        {
            get { return homeTerritory; }
        }

        protected virtual void Awake()
        {
            if (brain == null)
            {
                brain = GetComponent<EnemyBrain>();
            }

            if (homePosition == Vector3.zero)
            {
                homePosition = transform.position;
            }
        }

        protected virtual void Update()
        {
            if (automaticTick)
            {
                ManualTick(Time.deltaTime, homeTerritory);
            }
        }

        public virtual void Configure(
            EnemyBrain newBrain,
            Transform newTarget,
            TerritoryKind newHomeTerritory,
            Vector3 newHomePosition)
        {
            brain = newBrain != null ? newBrain : GetComponent<EnemyBrain>();
            target = newTarget;
            homeTerritory = newHomeTerritory;
            homePosition = newHomePosition;
            ControllerState = EnemyControllerState.Dormant;
            TargetDistance = 0f;
            TargetTerritoryAllowed = false;
        }

        public void SetAutomaticTick(bool enabled)
        {
            automaticTick = enabled;
        }

        protected bool TryTrackTargetOrReturn(
            float deltaSeconds,
            TerritoryKind targetTerritory,
            out Vector3 targetPosition,
            out float targetDistance)
        {
            targetPosition = transform.position;
            targetDistance = 0f;
            TargetDistance = 0f;

            var currentBrain = Brain;
            if (currentBrain == null)
            {
                TargetTerritoryAllowed = false;
                ControllerState = EnemyControllerState.Dormant;
                return false;
            }

            if (target == null)
            {
                TargetTerritoryAllowed = territoryRules.CanEnter(currentBrain.EnemyKind, targetTerritory);
                ReturnHome(deltaSeconds);
                return false;
            }

            TargetTerritoryAllowed = territoryRules.CanEnter(currentBrain.EnemyKind, targetTerritory);
            if (!TargetTerritoryAllowed)
            {
                ReturnHome(deltaSeconds);
                return false;
            }

            targetPosition = FlattenToSelfHeight(target.position);
            targetDistance = Vector3.Distance(transform.position, targetPosition);
            TargetDistance = targetDistance;
            ControllerState = EnemyControllerState.Tracking;
            return true;
        }

        protected void ReturnHome(float deltaSeconds)
        {
            var wasReturning = ControllerState == EnemyControllerState.ReturnHome;
            ControllerState = EnemyControllerState.ReturnHome;
            var destination = FlattenToSelfHeight(homePosition);
            var speed = Brain != null && Brain.Stats != null ? Brain.Stats.MoveSpeed : 1.8f;
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                Mathf.Max(0f, deltaSeconds) * speed);

            if (wasReturning && Vector3.Distance(transform.position, destination) <= returnArrivalDistance)
            {
                ControllerState = EnemyControllerState.Despawn;
                gameObject.SetActive(false);
            }
        }

        protected Vector3 FlattenToSelfHeight(Vector3 position)
        {
            return new Vector3(position.x, transform.position.y, position.z);
        }

        public abstract void ManualTick(float deltaSeconds, TerritoryKind targetTerritory);
    }
}
