using System;
using System.Collections.Generic;
using MathHighLow.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;

namespace MathHighLow.UI
{
    /// <summary>
    /// 코덱스 지시서에 맞춘 동적 UI 구성 및 이벤트 전달을 담당합니다.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        public event Action<CardDefinition, CardButtonView> OnNumberCardClicked;
        public event Action<OperatorType> OnOperatorSelected;
        public event Action OnSqrtSelected;
        public event Action OnSubmitRequested;
        public event Action OnResetRequested;
        public event Action<int> OnTargetSelected;
        public event Action OnBetIncreaseRequested;
        public event Action OnBetDecreaseRequested;

        [Header("프리팹 (선택 사항)")]
        [SerializeField] private CardButtonView numberCardPrefab;

        private RectTransform root;
        private Transform aiCardContainer;
        private Transform playerCardContainer;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI playerExpressionText;
        private TextMeshProUGUI aiExpressionText;
        private TextMeshProUGUI betValueText;
        private TextMeshProUGUI playerCreditsText;
        private TextMeshProUGUI aiCreditsText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI submitTooltipText;
        private TextMeshProUGUI multiplyBadgeText;
        private TextMeshProUGUI sqrtBadgeText;
        private TextMeshProUGUI resultSummaryText;
        private TextMeshProUGUI resultDetailText;
        private Button submitButton;
        private Button resetButton;
        private Button betIncreaseButton;
        private Button betDecreaseButton;
        private Button sqrtButton;
        private Button multiplyButton;
        private readonly Dictionary<OperatorType, Button> operatorButtons = new();
        private readonly Dictionary<Button, int> targetLookup = new();
        private readonly List<Button> targetButtons = new();
        private Transform targetButtonContainer;
        private readonly List<CardButtonView> playerCardViews = new();
        private readonly List<GameObject> aiCardViews = new();
        private GameObject disablePromptPanel;
        private TextMeshProUGUI disablePromptText;
        private readonly List<Button> disablePromptButtons = new();
        private bool layoutBuilt;

        private static readonly Vector2 DefaultCardSize = new(120f, 80f);

        private void Awake()
        {
            TMPFontSupportUtility.EnsureHangulSupport();
        }

        public void BuildLayout()
        {
            if (layoutBuilt)
            {
                return;
            }

            layoutBuilt = true;
            EnsureCanvasComponents();

            var rootGo = new GameObject("UIRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(40f, 40f);
            root.offsetMax = new Vector2(-40f, -40f);

            var background = rootGo.AddComponent<Image>();
            background.color = new Color(0.95f, 0.95f, 0.97f, 1f);

            var verticalLayout = rootGo.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 16f;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = false;

            BuildHeader(verticalLayout.transform);
            BuildTargetSection(verticalLayout.transform);
            BuildBadgeSection(verticalLayout.transform);
            BuildCardSections(verticalLayout.transform);
            BuildExpressionSection(verticalLayout.transform);
            BuildOperatorSection(verticalLayout.transform);
            BuildActionSection(verticalLayout.transform);
            BuildResultSection(verticalLayout.transform);
        }

        public void PrepareForRound()
        {
            ClearContainerChildren(aiCardContainer);
            ClearContainerChildren(playerCardContainer);
            playerCardViews.Clear();
            aiCardViews.Clear();
            UpdatePlayerExpression("수식을 구성 중...");
            UpdateAiExpression("AI 수식: -");
            SetStatusMessage("카드를 기다리는 중...");
            ShowRoundResult(string.Empty, string.Empty);
            SetSubmitInteractable(false, string.Empty);
            HideDisableOperatorPrompt();
            SetOperatorEnabled(OperatorType.Add, true);
            SetOperatorEnabled(OperatorType.Subtract, true);
            SetOperatorEnabled(OperatorType.Divide, true);
        }

        public void SetTargetOptions(IEnumerable<int> targets)
        {
            foreach (var button in targetButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            targetButtons.Clear();
            targetLookup.Clear();

            if (targets == null)
            {
                return;
            }

            if (targetButtonContainer == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                var button = CreateTextButton(targetButtonContainer, $"={target}", 28);
                button.onClick.AddListener(() => HandleTargetButtonClicked(button, target));
                targetButtons.Add(button);
                targetLookup[button] = target;
            }
        }

        public void HighlightTarget(int target)
        {
            foreach (var button in targetButtons)
            {
                if (button == null)
                {
                    continue;
                }

                var image = button.GetComponent<Image>();
                if (!targetLookup.TryGetValue(button, out var value) || image == null)
                {
                    continue;
                }

                image.color = value == target ? new Color(0.8f, 0.9f, 1f) : Color.white;
            }
        }

        public CardButtonView AddPlayerNumberCard(CardDefinition card)
        {
            var view = CreateCardView(playerCardContainer, card, true);
            playerCardViews.Add(view);
            return view;
        }

        public void AddAiCard(CardDefinition card)
        {
            var view = CreateCardView(aiCardContainer, card, false);
            if (view != null)
            {
                view.Interactable = false;
                aiCardViews.Add(view.gameObject);
            }
        }

        public void UpdatePlayerExpression(string text)
        {
            if (playerExpressionText != null)
            {
                playerExpressionText.text = string.IsNullOrEmpty(text) ? "플레이어 수식: -" : $"플레이어 수식: {text}";
            }
        }

        public void UpdateAiExpression(string text)
        {
            if (aiExpressionText != null)
            {
                aiExpressionText.text = string.IsNullOrEmpty(text) ? "AI 수식: -" : $"AI 수식: {text}";
            }
        }

        public void SetStatusMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void UpdateBetDisplay(int amount)
        {
            if (betValueText != null)
            {
                betValueText.text = $"${amount}";
            }
        }

        public void UpdateCredits(int player, int ai)
        {
            if (playerCreditsText != null)
            {
                playerCreditsText.text = $"플레이어 잔액: ${player}";
            }

            if (aiCreditsText != null)
            {
                aiCreditsText.text = $"AI 잔액: ${ai}";
            }
        }

        public void UpdateTimer(float elapsed, float duration, float unlockThreshold)
        {
            if (timerText == null)
            {
                return;
            }

            var remaining = Mathf.Max(0f, duration - elapsed);
            var minutes = Mathf.FloorToInt(remaining / 60f);
            var seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"타이머 {minutes:00}:{seconds:00}";

            var unlockRemaining = Mathf.Max(0f, unlockThreshold - elapsed);
            if (unlockRemaining > 0f)
            {
                submitTooltipText.text = $"제출 가능까지 {unlockRemaining:0}초";
            }

            if (remaining <= 30f)
            {
                var pulse = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
                timerText.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(1f, 0.6f, 0.6f), pulse);
            }
            else
            {
                timerText.color = Color.black;
            }
        }

        public void SetSubmitInteractable(bool interactable, string reason)
        {
            if (submitButton != null)
            {
                submitButton.interactable = interactable;
            }

            if (submitTooltipText != null)
            {
                submitTooltipText.text = interactable ? "제출 가능" : reason;
            }
        }

        public void UpdateSpecialBadges(int remainingMultiply, int remainingSqrt)
        {
            if (multiplyBadgeText != null)
            {
                multiplyBadgeText.text = $"× 남음: {remainingMultiply}";
            }

            if (sqrtBadgeText != null)
            {
                sqrtBadgeText.text = $"√ 남음: {remainingSqrt}";
            }

            if (multiplyButton != null)
            {
                multiplyButton.interactable = remainingMultiply > 0;
            }

            if (sqrtButton != null)
            {
                sqrtButton.interactable = remainingSqrt > 0;
            }
        }

        public void SetOperatorEnabled(OperatorType operatorType, bool enabled)
        {
            if (operatorButtons.TryGetValue(operatorType, out var button) && button != null)
            {
                button.interactable = enabled;
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                }
            }
        }

        public void SetDisableOperatorPrompt(string title, string message, string confirmLabel)
        {
            if (disablePromptText == null)
            {
                return;
            }

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(title))
            {
                parts.Add(title);
            }

            if (!string.IsNullOrEmpty(message))
            {
                parts.Add(message);
            }

            if (!string.IsNullOrEmpty(confirmLabel))
            {
                parts.Add(confirmLabel);
            }

            disablePromptText.text = parts.Count > 0
                ? string.Join("\n", parts)
                : string.Empty;
        }

        public void ShowDisableOperatorPrompt()
        {
            if (disablePromptPanel == null)
            {
                return;
            }

            disablePromptPanel.SetActive(true);
        }

        public void ShowDisableOperatorPrompt(IEnumerable<OperatorType> options, Action<OperatorType> onSelected)
        {
            if (disablePromptPanel == null)
            {
                return;
            }

            ShowDisableOperatorPrompt();
            disablePromptButtons.ForEach(button => Destroy(button.gameObject));
            disablePromptButtons.Clear();

            if (disablePromptText != null && string.IsNullOrEmpty(disablePromptText.text))
            {
                disablePromptText.text = "× 카드 효과: 비활성화할 기호를 선택하세요";
            }

            foreach (var option in options)
            {
                var button = CreateTextButton(disablePromptPanel.transform, option.ToSymbol(), 28);
                var captured = option;
                button.onClick.AddListener(() => onSelected?.Invoke(captured));
                disablePromptButtons.Add(button);
            }
        }

        public void HideDisableOperatorPrompt()
        {
            if (disablePromptPanel == null)
            {
                return;
            }

            disablePromptPanel.SetActive(false);
            disablePromptButtons.ForEach(button => Destroy(button.gameObject));
            disablePromptButtons.Clear();
            if (disablePromptText != null)
            {
                disablePromptText.text = string.Empty;
            }
        }

        public void ShowRoundResult(string summary, string detail)
        {
            if (resultSummaryText != null)
            {
                resultSummaryText.text = summary;
            }

            if (resultDetailText != null)
            {
                resultDetailText.text = detail;
            }
        }

        private void BuildHeader(Transform parent)
        {
            var container = CreateVerticalSection(parent, "Header Section");
            var title = CreateText(container, "Math High-Low", 46, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = new Color(0.1f, 0.1f, 0.18f);
            var info = CreateText(container, "모든 숫자/특수 카드를 사용하여 목표값(=1 또는 =20)에 가장 근접하세요.", 26, FontStyles.Normal, TextAlignmentOptions.Center);
            info.color = new Color(0.2f, 0.2f, 0.25f);
            statusText = CreateText(container, "게임을 준비하는 중...", 26, FontStyles.Italic, TextAlignmentOptions.Center);
        }

        private void BuildTargetSection(Transform parent)
        {
            var container = CreateVerticalSection(parent, "Target Section");
            CreateText(container, "목표 선택", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            targetButtonContainer = CreateHorizontalRow(container, "Target Buttons");
            targetButtons.Clear();
            targetLookup.Clear();
        }

        private void BuildBadgeSection(Transform parent)
        {
            var container = CreateHorizontalRow(parent, "Badge Row");
            multiplyBadgeText = CreateText(container, "× 남음: 0", 26, FontStyles.Normal, TextAlignmentOptions.Left);
            sqrtBadgeText = CreateText(container, "√ 남음: 0", 26, FontStyles.Normal, TextAlignmentOptions.Left);
            playerCreditsText = CreateText(container, "플레이어 잔액: $0", 26, FontStyles.Normal, TextAlignmentOptions.Right);
            aiCreditsText = CreateText(container, "AI 잔액: $0", 26, FontStyles.Normal, TextAlignmentOptions.Right);
        }

        private void BuildCardSections(Transform parent)
        {
            var aiSection = CreateVerticalSection(parent, "AI Cards");
            CreateText(aiSection, "AI 공개 패", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            aiCardContainer = CreateHorizontalRow(aiSection, "AI Card Row");

            var playerSection = CreateVerticalSection(parent, "Player Cards");
            CreateText(playerSection, "플레이어 패", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            playerCardContainer = CreateHorizontalRow(playerSection, "Player Card Row");
        }

        private void BuildExpressionSection(Transform parent)
        {
            var container = CreateVerticalSection(parent, "Expression Section");
            playerExpressionText = CreateText(container, "플레이어 수식: -", 28, FontStyles.Normal, TextAlignmentOptions.Left);
            aiExpressionText = CreateText(container, "AI 수식: -", 28, FontStyles.Normal, TextAlignmentOptions.Left);
        }

        private void BuildOperatorSection(Transform parent)
        {
            var container = CreateVerticalSection(parent, "Operator Section");
            CreateText(container, "기호 선택", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            var row = CreateHorizontalRow(container, "Operators");

            AddOperatorButton(row, OperatorType.Add, "+");
            AddOperatorButton(row, OperatorType.Subtract, "-");
            AddOperatorButton(row, OperatorType.Divide, "÷");

            multiplyButton = CreateTextButton(row, "×", 32);
            multiplyButton.onClick.AddListener(() => OnOperatorSelected?.Invoke(OperatorType.Multiply));

            sqrtButton = CreateTextButton(row, "√", 32);
            sqrtButton.onClick.AddListener(() => OnSqrtSelected?.Invoke());

            disablePromptPanel = CreatePanel(parent, "Disable Prompt");
            disablePromptPanel.SetActive(false);
            disablePromptText = CreateText(disablePromptPanel.transform, string.Empty, 24, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private void BuildActionSection(Transform parent)
        {
            var container = CreateVerticalSection(parent, "Action Section");
            CreateText(container, "라운드 제어", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            var row = CreateHorizontalRow(container, "Action Row");

            betDecreaseButton = CreateTextButton(row, "Bet -", 30);
            betDecreaseButton.onClick.AddListener(() => OnBetDecreaseRequested?.Invoke());

            betValueText = CreateText(row, "$0", 30, FontStyles.Bold, TextAlignmentOptions.Center);

            betIncreaseButton = CreateTextButton(row, "Bet +", 30);
            betIncreaseButton.onClick.AddListener(() => OnBetIncreaseRequested?.Invoke());

            timerText = CreateText(row, "타이머 03:00", 30, FontStyles.Bold, TextAlignmentOptions.Center);

            submitButton = CreateTextButton(row, "제출", 32);
            submitButton.onClick.AddListener(() => OnSubmitRequested?.Invoke());

            resetButton = CreateTextButton(row, "초기화", 28);
            resetButton.onClick.AddListener(() => OnResetRequested?.Invoke());

            submitTooltipText = CreateText(container, "제출 조건을 충족하세요.", 24, FontStyles.Italic, TextAlignmentOptions.Center);
        }

        private void BuildResultSection(Transform parent)
        {
            var container = CreateVerticalSection(parent, "Result Section");
            CreateText(container, "결과", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            resultSummaryText = CreateText(container, string.Empty, 30, FontStyles.Bold, TextAlignmentOptions.Left);
            resultDetailText = CreateText(container, string.Empty, 26, FontStyles.Normal, TextAlignmentOptions.Left);
        }

        private void AddOperatorButton(Transform parent, OperatorType operatorType, string label)
        {
            var button = CreateTextButton(parent, label, 32);
            button.onClick.AddListener(() => OnOperatorSelected?.Invoke(operatorType));
            operatorButtons[operatorType] = button;
        }

        private CardButtonView CreateCardView(Transform parent, CardDefinition card, bool interactable)
        {
            CardButtonView view;

            if (numberCardPrefab != null)
            {
                view = Instantiate(numberCardPrefab, parent);
            }
            else
            {
                var go = new GameObject("CardButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);

                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = DefaultCardSize;

                var image = go.GetComponent<Image>();
                image.color = Color.white;

                var button = go.GetComponent<Button>();
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.95f, 1f);
                colors.pressedColor = new Color(0.8f, 0.85f, 1f);
                button.colors = colors;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();

                label.fontSize = 36;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.black;

                var layout = go.AddComponent<LayoutElement>();
                layout.preferredWidth = rect.sizeDelta.x;
                layout.preferredHeight = rect.sizeDelta.y;

                view = go.AddComponent<CardButtonView>();
            }

            var callback = interactable
                ? new Action<CardButtonView>(handle => OnNumberCardClicked?.Invoke(handle.Card, handle))
                : null;

            view.Initialize(card, callback);
            view.Interactable = interactable;
            if (!interactable)
            {
                var button = view.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }

            EnsureLayoutElement(view);

            return view;
        }

        private void EnsureLayoutElement(CardButtonView view)
        {
            if (view == null)
            {
                return;
            }

            var layout = view.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = view.gameObject.AddComponent<LayoutElement>();
            }

            if (layout.preferredWidth <= 0f)
            {
                layout.preferredWidth = DefaultCardSize.x;
            }

            if (layout.preferredHeight <= 0f)
            {
                layout.preferredHeight = DefaultCardSize.y;
            }
        }

        private void HandleTargetButtonClicked(Button button, int target)
        {
            HighlightTarget(target);
            OnTargetSelected?.Invoke(target);
        }

        private Transform CreateVerticalSection(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            return go.transform;
        }

        private Transform CreateHorizontalRow(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            return go.transform;
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.black;

            var layout = go.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredHeight = Mathf.Max(40f, fontSize * 1.2f);

            return text;
        }

        private Button CreateTextButton(Transform parent, string label, int fontSize)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 72f);

            var image = go.GetComponent<Image>();
            image.color = Color.white;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f);
            colors.pressedColor = new Color(0.85f, 0.9f, 1f);
            button.colors = colors;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();

            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.text = label;
            if (text.rectTransform != null)
            {
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = Vector2.zero;
                text.rectTransform.offsetMax = Vector2.zero;
            }

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = rect.sizeDelta.x;
            layout.preferredHeight = rect.sizeDelta.y;

            return button;
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            var panelGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 140f);

            var image = panelGo.GetComponent<Image>();
            image.color = new Color(0.95f, 0.9f, 0.7f, 0.95f);

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(16, 16, 16, 16);

            return panelGo;
        }

        private void ClearContainerChildren(Transform container)
        {
            if (container == null)
            {
                return;
            }

            for (var i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void EnsureCanvasComponents()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

    }
}
