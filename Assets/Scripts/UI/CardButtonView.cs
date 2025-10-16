using System;
using System.Linq;
using MathHighLow.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathHighLow.UI
{
    /// <summary>
    /// 카드 버튼의 클릭 이벤트와 라벨링을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardButtonView : MonoBehaviour
    {
        [Header("텍스트 설정")]
        [SerializeField] private TMP_Text numberLabel;
        [SerializeField] private TMP_Text operatorLabel;
        [SerializeField] private Text legacyLabel;

        public enum Ownership
        {
            Player,
            Ai
        }

        [Header("비주얼 설정")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite numberSprite;
        [SerializeField] private Sprite operatorSprite;
        [SerializeField] private Vector2 numberLabelOffset = Vector2.zero;
        [SerializeField] private Vector2 operatorLabelOffset = Vector2.zero;
        [Header("색상 설정")]
        [Tooltip("플레이어 숫자 카드 배경색입니다. 알파가 0이면 원래 색을 유지합니다.")]
        [SerializeField] private Color playerNumberColor = Color.clear;
        [Tooltip("AI 숫자 카드 배경색입니다. 기본값은 연한 파란색입니다.")]
        [SerializeField] private Color aiNumberColor = new Color(0.85f, 0.92f, 1f);
        [Tooltip("플레이어 연산자/특수 카드 배경색입니다. 알파가 0이면 원래 색을 유지합니다.")]
        [SerializeField] private Color playerOperatorColor = Color.clear;
        [Tooltip("AI 연산자/특수 카드 배경색입니다. 알파가 0이면 원래 색을 유지합니다.")]
        [SerializeField] private Color aiOperatorColor = Color.clear;

        private Button button;
        private CardDefinition card;
        private Action<CardButtonView> onClicked;
        private TMP_Text fallbackLabel;
        private TMP_Text[] cachedTmpLabels = Array.Empty<TMP_Text>();
        private Sprite initialBackgroundSprite;
        private Color initialBackgroundColor = Color.white;
        private Ownership ownership = Ownership.Player;

        public CardDefinition Card => card;

        public bool Interactable
        {
            get => button != null && button.interactable;
            set
            {
                if (button != null)
                {
                    button.interactable = value;
                }
            }
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            CacheLabelReferences();
            if (backgroundImage != null)
            {
                initialBackgroundSprite = backgroundImage.sprite;
                initialBackgroundColor = backgroundImage.color;
            }
        }

        public void Initialize(CardDefinition definition, Action<CardButtonView> clicked, Ownership owner = Ownership.Player)
        {
            card = definition;
            onClicked = clicked;
            ownership = owner;
            CacheLabelReferences();
            ApplyCardVisual();
            ConfigureButton();
        }

        private void HandleClicked()
        {
            onClicked?.Invoke(this);
        }

        private void ConfigureButton()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClicked);

            if (onClicked != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void ApplyCardVisual()
        {
            if (card == null)
            {
                return;
            }

            var displayText = card.GetDisplayText();

            var activeLabel = ResolvePrimaryLabel();
            var inactiveLabel = ResolveSecondaryLabel(activeLabel);

            if (activeLabel != null)
            {
                activeLabel.gameObject.SetActive(true);
                activeLabel.text = displayText;
                PositionLabel(activeLabel.rectTransform, card.Kind == CardKind.Number);
            }

            if (inactiveLabel != null && inactiveLabel != activeLabel)
            {
                inactiveLabel.text = string.Empty;
                inactiveLabel.gameObject.SetActive(false);
            }

            UpdateLegacyLabel(activeLabel, displayText);
            UpdateBackground();
        }

        private void UpdateLegacyLabel(TMP_Text activeLabel, string displayText)
        {
            if (legacyLabel == null)
            {
                return;
            }

            if (activeLabel != null)
            {
                legacyLabel.gameObject.SetActive(false);
                legacyLabel.text = string.Empty;
                return;
            }

            legacyLabel.gameObject.SetActive(true);
            legacyLabel.text = displayText;
            if (legacyLabel.transform is RectTransform rect)
            {
                PositionLabel(rect, card.Kind == CardKind.Number);
            }
        }

        private void UpdateBackground()
        {
            if (backgroundImage == null)
            {
                return;
            }

            var isNumber = card.Kind == CardKind.Number;
            backgroundImage.sprite = isNumber
                ? (numberSprite != null ? numberSprite : initialBackgroundSprite)
                : (operatorSprite != null ? operatorSprite : initialBackgroundSprite);
            backgroundImage.color = ResolveBackgroundColor(isNumber);
        }

        private Color ResolveBackgroundColor(bool isNumber)
        {
            var preferred = ownership == Ownership.Player
                ? (isNumber ? playerNumberColor : playerOperatorColor)
                : (isNumber ? aiNumberColor : aiOperatorColor);

            if (preferred.a > 0f)
            {
                return preferred;
            }

            return initialBackgroundColor;
        }

        private TMP_Text ResolvePrimaryLabel()
        {
            if (card == null)
            {
                return null;
            }

            if (card.Kind == CardKind.Number)
            {
                return numberLabel != null ? numberLabel : fallbackLabel;
            }

            return operatorLabel != null ? operatorLabel : fallbackLabel;
        }

        private TMP_Text ResolveSecondaryLabel(TMP_Text activeLabel)
        {
            if (card == null)
            {
                return null;
            }

            if (card.Kind == CardKind.Number)
            {
                if (operatorLabel != null && operatorLabel != activeLabel)
                {
                    return operatorLabel;
                }
            }
            else
            {
                if (numberLabel != null && numberLabel != activeLabel)
                {
                    return numberLabel;
                }
            }

            return null;
        }

        private void PositionLabel(RectTransform rect, bool isNumber)
        {
            if (rect == null)
            {
                return;
            }

            var anchor = isNumber ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            var offset = isNumber ? numberLabelOffset : operatorLabelOffset;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
        }

        private void CacheLabelReferences()
        {
            cachedTmpLabels = GetComponentsInChildren<TMP_Text>(true);

            var numberCandidate = FindLabelByKeyword("number") ?? numberLabel;
            var operatorCandidate = FindLabelByKeyword("operator") ?? FindLabelByKeyword("symbol") ?? operatorLabel;

            numberLabel = EnsureUiCompatibleLabel(numberCandidate);
            operatorLabel = EnsureUiCompatibleLabel(operatorCandidate);

            if (fallbackLabel == null || fallbackLabel == numberLabel || fallbackLabel == operatorLabel)
            {
                fallbackLabel = EnsureUiCompatibleLabel(cachedTmpLabels.FirstOrDefault(text => text != null && text != numberLabel && text != operatorLabel));
            }
            else
            {
                fallbackLabel = EnsureUiCompatibleLabel(fallbackLabel);
            }

            if (fallbackLabel == null)
            {
                fallbackLabel = numberLabel ?? operatorLabel;
            }

            if (legacyLabel == null)
            {
                legacyLabel = GetComponentsInChildren<Text>(true).FirstOrDefault();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>() ?? GetComponentInChildren<Image>();
            }

            if (backgroundImage != null && initialBackgroundSprite == null)
            {
                initialBackgroundSprite = backgroundImage.sprite;
            }

            cachedTmpLabels = GetComponentsInChildren<TMP_Text>(true);
        }

        private TMP_Text FindLabelByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return null;
            }

            var label = cachedTmpLabels.FirstOrDefault(text => text is TextMeshProUGUI && text.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            if (label != null)
            {
                return label;
            }

            return cachedTmpLabels.FirstOrDefault(text => text != null && text.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private TMP_Text EnsureUiCompatibleLabel(TMP_Text label)
        {
            if (label == null)
            {
                return null;
            }

            if (label is TextMeshProUGUI)
            {
                return label;
            }

            var go = label.gameObject;
            if (go == null)
            {
                return label;
            }

            var originalText = label.text;
            var originalFont = label.font;
            var originalFontSize = label.fontSize;
            var originalAlignment = label.alignment;

            var uiLabel = go.GetComponent<TextMeshProUGUI>();
            if (uiLabel == null)
            {
                uiLabel = go.AddComponent<TextMeshProUGUI>();
            }

            if (uiLabel == null)
            {
                label.enabled = true;
                Debug.LogWarning($"[{nameof(CardButtonView)}] TextMeshProUGUI 컴포넌트를 {go.name} 오브젝트에 추가하지 못했습니다. 기존 TMP_Text를 계속 사용합니다.");
                return label;
            }

            uiLabel.text = originalText;
            if (originalFont != null)
            {
                uiLabel.font = originalFont;
            }
            uiLabel.fontSize = originalFontSize;
            uiLabel.alignment = originalAlignment;

            label.enabled = false;

            return uiLabel;
        }
    }
}
