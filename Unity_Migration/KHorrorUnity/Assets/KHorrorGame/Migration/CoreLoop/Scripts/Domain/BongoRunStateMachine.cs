using System;
using System.Collections.Generic;

namespace KHorrorGame.Migration
{
    public sealed class BongoTravelEventArgs : EventArgs
    {
        public GameMapId Destination { get; private set; }

        public BongoTravelEventArgs(GameMapId destination)
        {
            Destination = destination;
        }
    }

    [Serializable]
    public sealed class BongoRunStateMachine
    {
        public const float DefaultTravelSeconds = 0.55f;

        private readonly List<ArtifactDefinition> _pendingCargoItems = new List<ArtifactDefinition>();
        private readonly float _travelSeconds;
        private float _travelElapsed;

        public event EventHandler<BongoTravelEventArgs> TravelStarted;
        public event EventHandler<BongoTravelEventArgs> TravelCompleted;
        public event Action StateChanged;

        public Inventory PlayerInventory { get; private set; }
        public QuotaTracker Quota { get; private set; }
        public ResentmentTracker Resentment { get; private set; }
        public GameMapId CurrentMap { get; private set; }
        public GameMapId? TravelDestination { get; private set; }
        public bool IsTraveling { get; private set; }
        public GameMapId? SelectedRetrievalMap { get; private set; }
        public int MapTravelCount { get; private set; }
        public int PendingRecoveredValue { get; private set; }
        public bool BongoMonitorOpen { get; private set; }
        public IReadOnlyList<ArtifactDefinition> PendingCargoItems
        {
            get { return _pendingCargoItems; }
        }

        public BongoRunStateMachine(
            Inventory playerInventory,
            QuotaTracker quota,
            ResentmentTracker resentment,
            float travelSeconds = DefaultTravelSeconds)
        {
            if (playerInventory == null)
            {
                throw new ArgumentNullException("playerInventory");
            }

            if (quota == null)
            {
                throw new ArgumentNullException("quota");
            }

            if (resentment == null)
            {
                throw new ArgumentNullException("resentment");
            }

            PlayerInventory = playerInventory;
            Quota = quota;
            Resentment = resentment;
            CurrentMap = GameMapId.BongoHub;
            _travelSeconds = Math.Max(travelSeconds, 0f);
        }

        public bool OperateBongoTerminal()
        {
            BongoMonitorOpen = true;
            NotifyStateChanged();

            if (IsTraveling || CurrentMap == GameMapId.BongoTravel)
            {
                return false;
            }

            if (CurrentMap == GameMapId.BongoHub)
            {
                return TravelToRetrievalMap(GameMapId.JonggaEstate);
            }

            if (CurrentMap == GameMapId.JonggaEstate)
            {
                return ReturnToBongoHub();
            }

            if (CurrentMap == GameMapId.SettlementOffice)
            {
                return ReturnToBongoHub();
            }

            return false;
        }

        public bool ExtractPlayerInventory()
        {
            return false;
        }

        public bool SettleStoredCargo()
        {
            return false;
        }

        public bool DepartBongo()
        {
            return CurrentMap == GameMapId.BongoHub
                ? TravelToRetrievalMap(GameMapId.JonggaEstate)
                : ReturnToBongoHub();
        }

        public bool ReturnToBongoHub()
        {
            if (IsTraveling || CurrentMap == GameMapId.BongoTravel || CurrentMap == GameMapId.BongoHub)
            {
                return false;
            }

            return BeginBongoTravel(GameMapId.BongoHub);
        }

        public bool TravelToRetrievalMap(GameMapId mapId)
        {
            if (IsTraveling || CurrentMap == GameMapId.BongoTravel)
            {
                return false;
            }

            if (CurrentMap != GameMapId.BongoHub || mapId != GameMapId.JonggaEstate)
            {
                return false;
            }

            SelectedRetrievalMap = mapId;
            return BeginBongoTravel(mapId);
        }

        public bool TravelToSettlementMap()
        {
            if (IsTraveling || CurrentMap == GameMapId.BongoTravel)
            {
                return false;
            }

            if (CurrentMap != GameMapId.BongoHub || PendingRecoveredValue <= 0)
            {
                return false;
            }

            return BeginBongoTravel(GameMapId.SettlementOffice);
        }

        public bool BeginBongoTravel(GameMapId destination)
        {
            if (IsTraveling || destination == CurrentMap || !IsSupportedDestination(destination))
            {
                return false;
            }

            IsTraveling = true;
            TravelDestination = destination;
            CurrentMap = GameMapId.BongoTravel;
            _travelElapsed = 0f;
            BongoMonitorOpen = false;
            var handler = TravelStarted;
            if (handler != null)
            {
                handler(this, new BongoTravelEventArgs(destination));
            }

            NotifyStateChanged();
            return true;
        }

        public bool TickTravel(float deltaSeconds)
        {
            if (!IsTraveling || TravelDestination == null)
            {
                return false;
            }

            _travelElapsed += Math.Max(deltaSeconds, 0f);
            if (_travelElapsed < _travelSeconds)
            {
                return false;
            }

            CompleteBongoTravel();
            return true;
        }

        public void CompleteBongoTravel()
        {
            if (!IsTraveling || TravelDestination == null)
            {
                return;
            }

            var destination = TravelDestination.Value;
            IsTraveling = false;
            TravelDestination = null;
            CurrentMap = destination;
            MapTravelCount += 1;
            var handler = TravelCompleted;
            if (handler != null)
            {
                handler(this, new BongoTravelEventArgs(destination));
            }

            NotifyStateChanged();
        }

        public string TerminalScreenText()
        {
            if (IsTraveling || CurrentMap == GameMapId.BongoTravel)
            {
                return "[TRAVELING]\nPlease wait";
            }

            if (CurrentMap == GameMapId.BongoHub)
            {
                return "[E]\nJongga Estate";
            }

            if (CurrentMap == GameMapId.JonggaEstate)
            {
                return "[E]\nReturn";
            }

            if (CurrentMap == GameMapId.SettlementOffice)
            {
                return "[E]\nReturn";
            }

            return "[E]\nUse Terminal";
        }

        public string TerminalActionText()
        {
            if (IsTraveling || CurrentMap == GameMapId.BongoTravel)
            {
                return "Traveling";
            }

            if (CurrentMap == GameMapId.BongoHub)
            {
                return "Drive to Jongga estate";
            }

            if (CurrentMap == GameMapId.JonggaEstate)
            {
                return "Return to the van";
            }

            if (CurrentMap == GameMapId.SettlementOffice)
            {
                return "Return to the van hub";
            }

            return "Idle";
        }

        public string CurrentMapLabel()
        {
            switch (CurrentMap)
            {
                case GameMapId.JonggaEstate:
                    return "Location: Jongga estate";
                case GameMapId.SettlementOffice:
                    return "Location: Settlement office";
                case GameMapId.BongoTravel:
                    return "Location: In transit";
                default:
                    return "Location: Van hub";
            }
        }

        public string MonitorBodyText()
        {
            var cargoLine = PendingRecoveredValue > 0
                ? string.Format("Loaded cargo: {0}", PendingRecoveredValue)
                : "Loaded cargo: 0";

            return string.Format("Action: {0}\n", TerminalActionText()) +
                   string.Format("Quota: {0} / {1}\n", Quota.RecoveredValue, Quota.RequiredValue) +
                   string.Format("Pending: {0}\n", PendingRecoveredValue) +
                   CurrentMapLabel() + "\n" +
                   cargoLine;
        }

        private static bool IsSupportedDestination(GameMapId destination)
        {
            return destination == GameMapId.BongoHub
                   || destination == GameMapId.JonggaEstate
                   || destination == GameMapId.SettlementOffice;
        }

        private void NotifyStateChanged()
        {
            var handler = StateChanged;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
