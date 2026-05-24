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

        [Header("Cargo")]
        [SerializeField] private VanCargoHold hubCargoHold;
        [SerializeField] private VanCargoHold estateCargoHold;

        [Header("Run Rules")]
        [SerializeField] private int startingQuotaValue = 800;
        [SerializeField] private float travelSeconds = BongoRunStateMachine.DefaultTravelSeconds;
        [SerializeField] private float feedbackSeconds = 2f;

        private Inventory fallbackInventory;
        private float feedbackRemainingSeconds;

        public event Action<GameLoopController> StateChanged;

        public BongoRunStateMachine State { get; private set; }
        public ThreatSpawnGate ThreatGate { get; private set; }
        public string FeedbackMessage { get; private set; } = string.Empty;

        public Inventory PlayerInventory => player != null ? player.Inventory : fallbackInventory;
        public QuotaTracker Quota => State.Quota;
        public ResentmentTracker Resentment => State.Resentment;
        public int LoadedCargoValue => ResolveSettlementCargoHold()?.TotalCargoValue ?? 0;
        public int LoadedCargoCount => ResolveSettlementCargoHold()?.CargoCount ?? 0;

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
            TickFeedback(Time.deltaTime);
        }

        public bool OperateBongoTerminal()
        {
            if (CanSettleLoadedCargo())
            {
                return SettleLoadedCargo();
            }

            return State.OperateBongoTerminal();
        }

        public bool ExtractPlayerInventory()
        {
            var extracted = State.ExtractPlayerInventory();
            if (extracted && player != null)
            {
                player.RefreshHeldItemViews();
            }

            return extracted;
        }

        public bool SettleStoredCargo()
        {
            if (CanSettleLoadedCargo())
            {
                return SettleLoadedCargo();
            }

            var settled = State.PendingRecoveredValue;
            var succeeded = State.SettleStoredCargo();
            if (succeeded)
            {
                ShowSettlementFeedback(settled);
            }

            return succeeded;
        }

        public bool ReturnToBongoHub()
        {
            return State.ReturnToBongoHub();
        }

        public void ShowFeedback(string message)
        {
            FeedbackMessage = message ?? string.Empty;
            feedbackRemainingSeconds = string.IsNullOrEmpty(FeedbackMessage) ? 0f : Mathf.Max(feedbackSeconds, 0.01f);
            NotifyStateChanged();
        }

        public void RegisterArtifactPicked(ArtifactDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            ThreatGate.NotifyArtifactPicked(definition);
            State.Resentment.AddResentment(ResentmentGainFor(definition), definition.DisplayName);
            NotifyStateChanged();
        }

        public string TerminalScreenText()
        {
            if (State == null)
            {
                return "[E]\nUse Terminal";
            }

            if (!State.IsTraveling && State.CurrentMap == GameMapId.BongoHub && LoadedCargoValue > 0)
            {
                return "[E]\nSettle Cargo";
            }

            return State.TerminalScreenText();
        }

        public string TerminalActionText()
        {
            if (State == null)
            {
                return "Use terminal";
            }

            if (!State.IsTraveling && State.CurrentMap == GameMapId.BongoHub && LoadedCargoValue > 0)
            {
                return "Settle loaded cargo";
            }

            return State.TerminalActionText();
        }

        public string MonitorBodyText()
        {
            if (State == null)
            {
                return string.Empty;
            }

            return string.Format("Action: {0}\n", TerminalActionText()) +
                   string.Format("Quota: {0} / {1}\n", Quota.RecoveredValue, Quota.RequiredValue) +
                   string.Format("Loaded cargo: {0}\n", LoadedCargoValue) +
                   string.Format("Cargo count: {0}\n", LoadedCargoCount) +
                   State.CurrentMapLabel();
        }

        private int ResentmentGainFor(ArtifactDefinition definition)
        {
            var gain = definition.ResentmentGain;
            if (!definition.HasTag("shrine_item"))
            {
                return gain;
            }

            var maxStageValue = ResentmentTracker.MinimumValueForStage(ThreatStageProfile.MaxStage);
            return Math.Max(gain, maxStageValue - State.Resentment.CurrentValue);
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
            if (args.Destination == GameMapId.BongoHub)
            {
                TransferEstateCargoToHub();
            }

            MovePlayerTo(args.Destination);

            if (player != null)
            {
                player.SetMovementEnabled(true);
            }

            RefreshWorldScope();
        }

        private bool CanSettleLoadedCargo()
        {
            return State != null
                   && !State.IsTraveling
                   && State.CurrentMap == GameMapId.BongoHub
                   && LoadedCargoValue > 0;
        }

        private bool SettleLoadedCargo()
        {
            var cargoHold = ResolveSettlementCargoHold();
            if (cargoHold == null)
            {
                ShowFeedback("No loaded cargo");
                return false;
            }

            var settledValue = cargoHold.ConsumeSettledCargo();
            if (settledValue <= 0)
            {
                ShowFeedback("No loaded cargo");
                return false;
            }

            Quota.AddRecoveredValue(settledValue);
            ShowSettlementFeedback(settledValue);
            NotifyStateChanged();
            return true;
        }

        private void TransferEstateCargoToHub()
        {
            if (hubCargoHold == null || estateCargoHold == null || hubCargoHold == estateCargoHold)
            {
                return;
            }

            estateCargoHold.TransferCargoTo(hubCargoHold);
        }

        private VanCargoHold ResolveSettlementCargoHold()
        {
            if (State != null && State.CurrentMap == GameMapId.BongoHub && hubCargoHold != null)
            {
                return hubCargoHold;
            }

            if (hubCargoHold != null && hubCargoHold.CargoCount > 0)
            {
                return hubCargoHold;
            }

            if (estateCargoHold != null && estateCargoHold.CargoCount > 0)
            {
                return estateCargoHold;
            }

            return hubCargoHold != null ? hubCargoHold : estateCargoHold;
        }

        private void ShowSettlementFeedback(int value)
        {
            ShowFeedback(string.Format("Settled +{0}", value));
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

        private void TickFeedback(float deltaSeconds)
        {
            if (string.IsNullOrEmpty(FeedbackMessage) || feedbackRemainingSeconds <= 0f)
            {
                return;
            }

            feedbackRemainingSeconds -= Mathf.Max(deltaSeconds, 0f);
            if (feedbackRemainingSeconds > 0f)
            {
                return;
            }

            FeedbackMessage = string.Empty;
            NotifyStateChanged();
        }
    }
}
