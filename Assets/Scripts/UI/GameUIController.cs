using System;
using System.Collections.Generic;
using MathHighLow.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Button disablePromptButtonPrefab;

        [Header("루트 및 컨테이너")]
        [SerializeField] private Transform aiCardContainer;
        [SerializeField] private Transform playerCardContainer;

        [Header("텍스트 필드")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerExpressionText;
        [SerializeField] private TextMeshProUGUI aiExpressionText;
        [SerializeField] private TextMeshProUGUI betValueText;
        [SerializeField] private TextMeshProUGUI playerCreditsText;
        [SerializeField] private TextMeshProUGUI aiCreditsText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI submitTooltipText;
        [SerializeField] private TextMeshProUGUI multiplyBadgeText;
        [SerializeField] private TextMeshProUGUI sqrtBadgeText;
        [SerializeField] private TextMeshProUGUI resultSummaryText;
        [SerializeField] private TextMeshProUGUI resultDetailText;
        [SerializeField] private TextMeshProUGUI disablePromptText;

        [Header("일반 버튼")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button betIncreaseButton;
        [SerializeField] private Button betDecreaseButton;
        [SerializeField] private Button sqrtButton;
        [SerializeField] private Button multiplyButton;

        [Header("타겟 버튼")]
        [SerializeField] private List<TargetButtonBinding> targetButtonBindings = new();

        [Header("연산자 버튼")]
        [SerializeField] private List<OperatorButtonBinding> operatorButtonBindings = new();

        [Header("비활성화 패널")]
        [SerializeField] private GameObject disablePromptPanel;
        [SerializeField] private Transform disablePromptButtonContainer;

        private readonly Dictionary<OperatorType, Button> operatorButtons = new();
        private readonly Dictionary<Button, int> targetLookup = new();
        private readonly List<Button> targetButtons = new();
        private readonly List<CardButtonView> playerCardViews = new();
        private readonly List<GameObject> aiCardViews = new();
        private readonly List<Button> disablePromptButtons = new();
        private bool layoutBuilt;

        [Serializable]
        private class TargetButtonBinding
        {
            public Button button;
            public TextMeshProUGUI label;
        }

        [Serializable]
        private class OperatorButtonBinding
        {
            public OperatorType operatorType;
            public Button button;
        }

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
            ConfigurePrimaryButtons();
            ConfigureOperatorButtons();
            ConfigureTargetBindings();
            HideDisableOperatorPrompt();
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
            targetLookup.Clear();

            if (targets == null)
            {
                foreach (var binding in targetButtonBindings)
                {
                    if (binding?.button == null)
                    {
                        continue;
                    }

                    binding.button.gameObject.SetActive(false);
                }

                return;
            }

            var targetList = targets is List<int> list ? list : new List<int>(targets);

            for (var i = 0; i < targetButtonBindings.Count; i++)
            {
                var binding = targetButtonBindings[i];
                if (binding?.button == null)
                {
                    continue;
                }

                if (i < targetList.Count)
                {
                    var target = targetList[i];
                    targetLookup[binding.button] = target;
                    binding.button.gameObject.SetActive(true);

                    if (binding.label == null)
                    {
                        binding.label = binding.button.GetComponentInChildren<TextMeshProUGUI>();
                    }

                    if (binding.label != null)
                    {
                        binding.label.text = $"={target}";
                    }
                }
                else
                {
                    binding.button.gameObject.SetActive(false);
                }
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
            ClearDisablePromptButtons();

            if (disablePromptText != null && string.IsNullOrEmpty(disablePromptText.text))
            {
                disablePromptText.text = "× 카드 효과: 비활성화할 기호를 선택하세요";
            }

            if (disablePromptButtonPrefab == null)
            {
                return;
            }

            var parent = disablePromptButtonContainer != null
                ? disablePromptButtonContainer
                : disablePromptPanel.transform;

            foreach (var option in options)
            {
                var button = Instantiate(disablePromptButtonPrefab, parent);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = option.ToSymbol();
                }

                var captured = option;
                button.onClick.RemoveAllListeners();
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
            ClearDisablePromptButtons();
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

        private void ConfigurePrimaryButtons()
        {
            ConfigureButton(submitButton, () => OnSubmitRequested?.Invoke());
            ConfigureButton(resetButton, () => OnResetRequested?.Invoke());
            ConfigureButton(betIncreaseButton, () => OnBetIncreaseRequested?.Invoke());
            ConfigureButton(betDecreaseButton, () => OnBetDecreaseRequested?.Invoke());
        }

        private void ConfigureOperatorButtons()
        {
            operatorButtons.Clear();

            foreach (var binding in operatorButtonBindings)
            {
                if (binding?.button == null)
                {
                    continue;
                }

                var capturedType = binding.operatorType;
                operatorButtons[capturedType] = binding.button;
                binding.button.onClick.RemoveAllListeners();
                binding.button.onClick.AddListener(() => OnOperatorSelected?.Invoke(capturedType));
            }

            if (multiplyButton != null)
            {
                multiplyButton.onClick.RemoveAllListeners();
                multiplyButton.onClick.AddListener(() => OnOperatorSelected?.Invoke(OperatorType.Multiply));
            }

            if (sqrtButton != null)
            {
                sqrtButton.onClick.RemoveAllListeners();
                sqrtButton.onClick.AddListener(() => OnSqrtSelected?.Invoke());
            }
        }

        private void ConfigureTargetBindings()
        {
            targetButtons.Clear();
            targetLookup.Clear();

            foreach (var binding in targetButtonBindings)
            {
                if (binding?.button == null)
                {
                    continue;
                }

                if (binding.label == null)
                {
                    binding.label = binding.button.GetComponentInChildren<TextMeshProUGUI>();
                }

                binding.button.onClick.RemoveAllListeners();
                binding.button.onClick.AddListener(() => HandleTargetButtonClicked(binding.button));
                binding.button.gameObject.SetActive(false);
                targetButtons.Add(binding.button);
            }
        }

        private void ConfigureButton(Button button, Action onClicked)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke());
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

        private void HandleTargetButtonClicked(Button button)
        {
            if (button == null)
            {
                return;
            }

            if (!targetLookup.TryGetValue(button, out var target))
            {
                return;
            }

            HighlightTarget(target);
            OnTargetSelected?.Invoke(target);
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

        private void ClearDisablePromptButtons()
        {
            foreach (var button in disablePromptButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            disablePromptButtons.Clear();
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
