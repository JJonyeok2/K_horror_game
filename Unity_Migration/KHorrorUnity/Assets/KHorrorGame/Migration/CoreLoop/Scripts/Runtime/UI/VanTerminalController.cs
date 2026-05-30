using UnityEngine;
using UnityEngine.UI;

namespace KHorrorGame.Migration
{
    public enum VanTerminalVisualState
    {
        Idle,
        ReadyToSettle,
        Traveling,
        Success,
        Failure
    }

    public sealed class VanTerminalController : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text footerText;
        [SerializeField] private Image panelBackplate;
        [SerializeField] private Image statusStrip;
        [SerializeField] private float charactersPerSecond = 42f;

        private string targetBodyText = string.Empty;
        private float visibleCharacters;
        private GameLoopController subscribedGameLoop;

        public string TargetBodyText => targetBodyText;
        public string TitleText => titleText != null ? titleText.text : string.Empty;
        public string VisibleBodyText => bodyText != null ? bodyText.text : string.Empty;
        public string FooterText => footerText != null ? footerText.text : string.Empty;
        public VanTerminalVisualState VisualState { get; private set; }

        public static VanTerminalController EnsureRuntimePanel(GameLoopController source)
        {
            var existing = FindObjectOfType<VanTerminalController>();
            if (existing != null)
            {
                existing.BindGameLoop(source, false);
                return existing;
            }

            var terminalObject = new GameObject("VanTerminalRuntimePanel");
            var controller = terminalObject.AddComponent<VanTerminalController>();
            controller.BindGameLoop(source, true);
            return controller;
        }

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            EnsureUi();
        }

        private void OnEnable()
        {
            SubscribeToGameLoop(gameLoop);
            ManualRefresh(true);
        }

        private void OnDisable()
        {
            if (subscribedGameLoop != null)
            {
                subscribedGameLoop.StateChanged -= OnGameLoopChanged;
                subscribedGameLoop = null;
            }
        }

        private void Update()
        {
            ManualTick(Time.deltaTime);
        }

        public void ManualRefresh(bool restartTypewriter)
        {
            var nextBody = BuildBodyText();
            if (restartTypewriter || nextBody != targetBodyText)
            {
                visibleCharacters = 0f;
            }

            targetBodyText = nextBody;
            VisualState = ResolveVisualState();
            ApplyVisualState();
            ApplyText();
        }

        public void ManualTick(float deltaTime)
        {
            if (targetBodyText.Length <= 0)
            {
                ApplyText();
                return;
            }

            visibleCharacters = Mathf.Min(
                targetBodyText.Length,
                visibleCharacters + Mathf.Max(charactersPerSecond, 1f) * Mathf.Max(deltaTime, 0f));
            ApplyText();
        }

        private void OnGameLoopChanged(GameLoopController controller)
        {
            BindGameLoop(controller, true);
        }

        private void BindGameLoop(GameLoopController source, bool restartTypewriter)
        {
            if (source != null)
            {
                gameLoop = source;
            }
            else if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            EnsureUi();
            SubscribeToGameLoop(gameLoop);
            ManualRefresh(restartTypewriter);
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

        private string BuildBodyText()
        {
            if (gameLoop == null || gameLoop.State == null)
            {
                return "SYSTEM: OFFLINE\nCARGO VALUE: 0\nCARGO COUNT: 0\nQUOTA: 0 / 0\nREMAINING: 0";
            }

            var loadedValue = gameLoop.LoadedCargoValue;
            var recovered = gameLoop.Quota.RecoveredValue;
            var required = gameLoop.Quota.RequiredValue;
            var remaining = Mathf.Max(required - recovered - loadedValue, 0);

            return "SYSTEM: K-BONGO CARGO TERMINAL\n" +
                   "CARGO VALUE: " + loadedValue + "\n" +
                   "CARGO COUNT: " + gameLoop.LoadedCargoCount + "\n" +
                   "QUOTA: " + recovered + " / " + required + "\n" +
                   "REMAINING: " + remaining + "\n" +
                   "ACTION: " + gameLoop.TerminalActionText();
        }

        private VanTerminalVisualState ResolveVisualState()
        {
            if (gameLoop == null || gameLoop.State == null)
            {
                return VanTerminalVisualState.Failure;
            }

            var feedback = gameLoop.FeedbackMessage ?? string.Empty;
            if (feedback.StartsWith("Settled", System.StringComparison.OrdinalIgnoreCase))
            {
                return VanTerminalVisualState.Success;
            }

            if (feedback.StartsWith("No ", System.StringComparison.OrdinalIgnoreCase) ||
                feedback.StartsWith("Cannot", System.StringComparison.OrdinalIgnoreCase) ||
                feedback.Contains("missing"))
            {
                return VanTerminalVisualState.Failure;
            }

            if (gameLoop.State.IsTraveling)
            {
                return VanTerminalVisualState.Traveling;
            }

            return gameLoop.LoadedCargoValue > 0
                ? VanTerminalVisualState.ReadyToSettle
                : VanTerminalVisualState.Idle;
        }

        private void ApplyText()
        {
            if (titleText != null)
            {
                titleText.text = BuildTitleText();
            }

            if (bodyText != null)
            {
                var length = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, targetBodyText.Length);
                bodyText.text = targetBodyText.Substring(0, length);
            }

            if (footerText != null)
            {
                footerText.text = FooterForState();
            }
        }

        private string FooterForState()
        {
            switch (VisualState)
            {
                case VanTerminalVisualState.ReadyToSettle:
                    return "READY // SETTLE CARGO";
                case VanTerminalVisualState.Traveling:
                    return "DRIVE SEQUENCE ACTIVE";
                case VanTerminalVisualState.Success:
                    return "SETTLED // QUOTA UPDATED";
                case VanTerminalVisualState.Failure:
                    return "CHECK CARGO // ACTION DENIED";
                default:
                    return "IDLE // WAITING FOR INPUT";
            }
        }

        private string BuildTitleText()
        {
            return gameLoop != null && gameLoop.State != null
                ? "K-BONGO CARGO TERMINAL"
                : "K-BONGO TERMINAL OFFLINE";
        }

        private void ApplyVisualState()
        {
            var color = ColorForState(VisualState);
            if (statusStrip != null)
            {
                statusStrip.color = color;
            }

            if (panelBackplate != null)
            {
                panelBackplate.color = new Color(0.025f, 0.035f, 0.03f, 0.88f);
            }
        }

        private static Color ColorForState(VanTerminalVisualState state)
        {
            switch (state)
            {
                case VanTerminalVisualState.ReadyToSettle:
                    return new Color(0.2f, 0.92f, 0.48f, 0.95f);
                case VanTerminalVisualState.Traveling:
                    return new Color(0.45f, 0.65f, 1f, 0.95f);
                case VanTerminalVisualState.Success:
                    return new Color(0.65f, 1f, 0.55f, 0.95f);
                case VanTerminalVisualState.Failure:
                    return new Color(1f, 0.23f, 0.18f, 0.95f);
                default:
                    return new Color(0.36f, 0.85f, 0.68f, 0.86f);
            }
        }

        private void EnsureUi()
        {
            if (titleText != null && bodyText != null && footerText != null)
            {
                return;
            }

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("VanTerminalCanvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var root = canvas.transform;
            panelBackplate = panelBackplate != null
                ? panelBackplate
                : CreateImage("TerminalBackplate", root, new Vector2(-24f, -24f), new Vector2(500f, 236f), new Color(0.025f, 0.035f, 0.03f, 0.88f));
            statusStrip = statusStrip != null
                ? statusStrip
                : CreateImage("TerminalStatusStrip", panelBackplate.transform, new Vector2(0f, -91f), new Vector2(452f, 8f), ColorForState(VanTerminalVisualState.Idle));
            titleText = titleText != null
                ? titleText
                : CreateText("TerminalTitleText", panelBackplate.transform, Vector2.zero, TextAnchor.MiddleCenter, 21, new Vector2(452f, 42f));
            bodyText = bodyText != null
                ? bodyText
                : CreateText("TerminalBodyText", panelBackplate.transform, Vector2.zero, TextAnchor.UpperLeft, 15, new Vector2(452f, 112f));
            footerText = footerText != null
                ? footerText
                : CreateText("TerminalFooterText", panelBackplate.transform, Vector2.zero, TextAnchor.MiddleCenter, 14, new Vector2(452f, 26f));

            ConfigurePanelTextRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(452f, 38f));
            ConfigurePanelTextRect(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -60f), new Vector2(452f, 112f));
            ConfigurePanelTextRect(footerText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(452f, 26f));
            ConfigurePanelTextRect(statusStrip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(452f, 8f));
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigurePanelTextRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = new Color(0.68f, 1f, 0.78f, 0.95f);
            text.raycastTarget = false;
            return text;
        }
    }
}
