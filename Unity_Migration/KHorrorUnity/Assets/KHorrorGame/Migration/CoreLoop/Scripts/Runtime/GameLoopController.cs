using System;
using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class GameLoopController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private UnityPlayerController player;
        [SerializeField] private Transform bongoHubSpawn;
        [SerializeField] private Transform estateSpawn;
        [SerializeField] private Transform settlementSpawn;

        [Header("World Roots")]
        [SerializeField] private GameObject bongoHubRoot;
        [SerializeField] private GameObject estateRoot;
        [SerializeField] private GameObject settlementRoot;
        [SerializeField] private GameObject travelRoot;

        [Header("Run Rules")]
        [SerializeField] private int startingQuotaValue = 800;
        [SerializeField] private float travelSeconds = BongoRunStateMachine.DefaultTravelSeconds;

        private Inventory fallbackInventory;

        public event Action<GameLoopController> StateChanged;

        public BongoRunStateMachine State { get; private set; }
        public ThreatSpawnGate ThreatGate { get; private set; }

        public Inventory PlayerInventory => player != null ? player.Inventory : fallbackInventory;
        public QuotaTracker Quota => State.Quota;
        public ResentmentTracker Resentment => State.Resentment;

        private void Awake()
        {
            if (player == null)
            {
                player = FindObjectOfType<UnityPlayerController>();
            }

            fallbackInventory = new Inventory(12f, 2);
            ThreatGate = new ThreatSpawnGate();
            State = new BongoRunStateMachine(
                PlayerInventory,
                new QuotaTracker(startingQuotaValue),
                new ResentmentTracker(),
                travelSeconds);

            State.TravelStarted += OnTravelStarted;
            State.TravelCompleted += OnTravelCompleted;
            State.CargoStored += OnCargoStored;
            State.SettlementCompleted += OnSettlementCompleted;
            State.StateChanged += NotifyStateChanged;
        }

        private void Start()
        {
            MovePlayerTo(State.CurrentMap);
            RefreshWorldScope();
            NotifyStateChanged();
        }

        private void Update()
        {
            State.TickTravel(Time.deltaTime);
            ThreatGate.Tick(Time.deltaTime);
        }

        public bool OperateBongoTerminal()
        {
            return State.OperateBongoTerminal();
        }

        public bool ExtractPlayerInventory()
        {
            return State.ExtractPlayerInventory();
        }

        public bool SettleStoredCargo()
        {
            return State.SettleStoredCargo();
        }

        public bool ReturnToBongoHub()
        {
            return State.ReturnToBongoHub();
        }

        public void RegisterArtifactPicked(ArtifactDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            ThreatGate.NotifyArtifactPicked(definition);
            State.Resentment.AddResentment(definition.ResentmentGain, definition.DisplayName);
            NotifyStateChanged();
        }

        private void OnTravelStarted(object sender, BongoTravelEventArgs args)
        {
            if (player != null)
            {
                player.SetMovementEnabled(false);
            }

            RefreshWorldScope();
        }

        private void OnTravelCompleted(object sender, BongoTravelEventArgs args)
        {
            MovePlayerTo(args.Destination);

            if (player != null)
            {
                player.SetMovementEnabled(true);
            }

            RefreshWorldScope();
        }

        private void OnCargoStored(object sender, CargoEventArgs args)
        {
            NotifyStateChanged();
        }

        private void OnSettlementCompleted(object sender, CargoEventArgs args)
        {
            NotifyStateChanged();
        }

        private void MovePlayerTo(GameMapId mapId)
        {
            if (player == null)
            {
                return;
            }

            Transform target = null;
            switch (mapId)
            {
                case GameMapId.JonggaEstate:
                    target = estateSpawn;
                    break;
                case GameMapId.SettlementOffice:
                    target = settlementSpawn;
                    break;
                default:
                    target = bongoHubSpawn;
                    break;
            }

            if (target != null)
            {
                player.Teleport(target.position, target.rotation);
            }
        }

        private void RefreshWorldScope()
        {
            var current = State.CurrentMap;
            SetActive(bongoHubRoot, current == GameMapId.BongoHub || current == GameMapId.BongoTravel);
            SetActive(estateRoot, current == GameMapId.JonggaEstate);
            SetActive(settlementRoot, current == GameMapId.SettlementOffice);
            SetActive(travelRoot, current == GameMapId.BongoTravel);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }
    }
}
