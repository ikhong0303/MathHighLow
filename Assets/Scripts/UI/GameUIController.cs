using System;
using System.Collections.Generic;
using MathHighLow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MathHighLow.UI
{
    /// <summary>
    /// 프로토타입용 UI를 생성하고 상호작용 이벤트를 제공하는 컨트롤러.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        public event Action<CardDefinition, CardButtonView> OnPlayerCardClicked;
        public event Action<int> OnTargetSelected;
        public event Action OnSubmitRequested;
        public event Action OnResetRequested;

        [Header("카드 프리팹 (선택 사항)")]
        [SerializeField] private CardButtonView numberCardPrefab;
        [SerializeField] private CardButtonView operatorCardPrefab;
        [SerializeField] private string numberCardResourcePath = "UI/NumberCardButton";
        [SerializeField] private string operatorCardResourcePath = "UI/OperatorCardButton";

        private RectTransform root;
        private Transform aiHandContainer;
        private Transform playerHandContainer;
        private Transform playerExpressionContainer;
        private Transform targetButtonContainer;
        private Transform actionButtonContainer;
        private Text statusText;
        private Text playerExpressionText;
        private Text aiExpressionText;
        private readonly List<CardButtonView> playerCardViews = new();
        private readonly List<GameObject> aiCardObjects = new();
        private readonly List<Button> targetButtons = new();
        private readonly Dictionary<Button, int> targetLookup = new();
        private Button submitButton;
        private Button resetButton;
        private Font defaultFont;
        private bool layoutBuilt;
        private static readonly Vector2 DefaultCardSize = new Vector2(120f, 80f);

        private void Awake()
        {
            LoadPrefabIfNeeded(ref numberCardPrefab, numberCardResourcePath);
            LoadPrefabIfNeeded(ref operatorCardPrefab, operatorCardResourcePath);
        }

        public void BuildLayout()
        {
            if (layoutBuilt)
            {
                return;
            }

            layoutBuilt = true;

            EnsureCanvasComponents();
            EnsureDefaultFont();

            var rootGo = new GameObject("UILayout", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(40f, 40f);
            root.offsetMax = new Vector2(-40f, -40f);

            var background = rootGo.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.05f);

            var layoutGroup = rootGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 16f;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;

            CreateHeader(layoutGroup.transform);
            aiHandContainer = CreateCardSection(layoutGroup.transform, "AI 카드");
            CreateExpressionSection(layoutGroup.transform);
            playerHandContainer = CreateCardSection(layoutGroup.transform, "플레이어 카드");
            targetButtonContainer = CreateButtonRow(layoutGroup.transform, "목표 선택");
            actionButtonContainer = CreateButtonRow(layoutGroup.transform, "조작");
            CreateActionButtons();
        }

        public void PrepareForRound()
        {
            ResetPlayerCards();
            ResetAiCards();
            UpdatePlayerExpression(string.Empty);
            UpdateAiExpression(string.Empty);
        }

        public void SetTargetOptions(IEnumerable<int> targets)
        {
            ClearTargetButtons();

            if (targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                var button = CreateTextButton(targetButtonContainer, $"={target}", 30);
                targetButtons.Add(button);
                targetLookup[button] = target;
                var capturedTarget = target;
                button.onClick.AddListener(() => HandleTargetButtonClicked(button, capturedTarget));
            }
        }

        public void HighlightTarget(int target)
        {
            foreach (var button in targetButtons)
            {
                var image = button.GetComponent<Image>();
                if (targetLookup.TryGetValue(button, out var value) && image != null)
                {
                    image.color = value == target ? new Color(0.8f, 0.9f, 1f) : Color.white;
                }
            }
        }

        public void AddAiCard(CardDefinition card)
        {
            var view = CreateCardButton(aiHandContainer, card, false);
            view.Interactable = false;
            aiCardObjects.Add(view.gameObject);
        }

        public void AddPlayerCard(CardDefinition card)
        {
            var view = CreateCardButton(playerHandContainer, card, true);
            playerCardViews.Add(view);
        }

        public void ResetPlayerCards()
        {
            ClearContainerChildren(playerHandContainer);
            ClearContainerChildren(playerExpressionContainer);
            playerCardViews.Clear();
        }

        private void ResetAiCards()
        {
            ClearContainerChildren(aiHandContainer);
            aiCardObjects.Clear();
        }

        public void UpdatePlayerExpression(string expression)
        {
            playerExpressionText.text = string.IsNullOrEmpty(expression)
                ? "플레이어 수식: -"
                : $"플레이어 수식: {expression}";
        }

        public void UpdateAiExpression(string expression)
        {
            aiExpressionText.text = string.IsNullOrEmpty(expression)
                ? "AI 수식: -"
                : $"AI 수식: {expression}";
        }

        public void SetStatusMessage(string message)
        {
            statusText.text = message;
        }

        private void CreateHeader(Transform parent)
        {
            var title = CreateText(parent, "Math High-Low Prototype", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            var instructions = CreateText(parent,
                "카드를 순서대로 선택하여 수식을 완성하고, 목표값에 더 가깝게 만들면 승리합니다.",
                24,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            statusText = CreateText(parent, "게임을 불러오는 중...", 26, FontStyle.Normal, TextAnchor.MiddleCenter);

            title.color = new Color(0.1f, 0.1f, 0.1f);
            instructions.color = new Color(0.1f, 0.1f, 0.1f);
            statusText.color = new Color(0.05f, 0.05f, 0.05f);
        }

        private Transform CreateCardSection(Transform parent, string header)
        {
            var section = new GameObject($"{header} Section", typeof(RectTransform));
            section.transform.SetParent(parent, false);

            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            CreateText(section.transform, header, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            var row = new GameObject("Cards", typeof(RectTransform));
            row.transform.SetParent(section.transform, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10f;
            horizontal.childControlWidth = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandHeight = false;

            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 100f;

            return row.transform;
        }

        private void CreateExpressionSection(Transform parent)
        {
            var section = new GameObject("Expression Section", typeof(RectTransform));
            section.transform.SetParent(parent, false);

            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            playerExpressionText = CreateText(section.transform, "플레이어 수식: -", 28, FontStyle.Normal, TextAnchor.MiddleLeft);

            var cardsRow = new GameObject("Expression Cards", typeof(RectTransform));
            cardsRow.transform.SetParent(section.transform, false);

            var horizontal = cardsRow.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 16f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandHeight = false;

            var layoutElement = cardsRow.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 120f;
            layoutElement.flexibleWidth = 1f;

            playerExpressionContainer = cardsRow.transform;

            aiExpressionText = CreateText(section.transform, "AI 수식: -", 28, FontStyle.Normal, TextAnchor.MiddleLeft);
        }

        private Transform CreateButtonRow(Transform parent, string header)
        {
            var section = new GameObject($"{header} Row", typeof(RectTransform));
            section.transform.SetParent(parent, false);

            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateText(section.transform, header, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            var row = new GameObject("Buttons", typeof(RectTransform));
            row.transform.SetParent(section.transform, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 12f;
            horizontal.childControlWidth = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandHeight = false;

            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 90f;

            return row.transform;
        }

        private void CreateActionButtons()
        {
            submitButton = CreateTextButton(actionButtonContainer, "수식 제출", 32);
            submitButton.onClick.AddListener(() => OnSubmitRequested?.Invoke());

            resetButton = CreateTextButton(actionButtonContainer, "선택 초기화", 32);
            resetButton.onClick.AddListener(() => OnResetRequested?.Invoke());
        }

        private Text CreateText(Transform parent, string content, int fontSize, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = defaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.black;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = Mathf.Max(40f, fontSize * 1.2f);
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1f;

            return text;
        }

        private Button CreateTextButton(Transform parent, string label, int fontSize)
        {
            var buttonGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 72f);

            var image = buttonGo.GetComponent<Image>();
            image.color = Color.white;

            var button = buttonGo.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f);
            colors.pressedColor = new Color(0.8f, 0.85f, 1f);
            colors.selectedColor = new Color(0.9f, 0.95f, 1f);
            button.colors = colors;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = defaultFont;
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            var layoutElement = buttonGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = rect.sizeDelta.x;
            layoutElement.preferredHeight = rect.sizeDelta.y;

            return button;
        }

        private CardButtonView CreateCardButton(Transform parent, CardDefinition card, bool interactable)
        {
            var template = card.Kind == CardKind.Number ? numberCardPrefab : operatorCardPrefab;
            CardButtonView view;

            if (template != null)
            {
                view = Instantiate(template, parent);
                view.gameObject.name = $"{card.GetDisplayText()} Card";
                view.transform.localScale = Vector3.one;
                EnsureLayoutElement(view);
            }
            else
            {
                view = CreateRuntimeCardButton(parent);
            }

            ConfigureCardView(view, card, interactable);

            return view;
        }

        public void MovePlayerCardToExpression(CardButtonView view)
        {
            if (view == null || playerExpressionContainer == null)
            {
                return;
            }

            view.transform.SetParent(playerExpressionContainer, true);
            view.transform.SetAsLastSibling();

            if (view.transform is RectTransform rect)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }
        }

        private CardButtonView CreateRuntimeCardButton(Transform parent)
        {
            var buttonGo = new GameObject("CardButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.sizeDelta = DefaultCardSize;

            var image = buttonGo.GetComponent<Image>();
            image.color = Color.white;

            var button = buttonGo.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f);
            colors.pressedColor = new Color(0.8f, 0.85f, 1f);
            colors.selectedColor = new Color(0.85f, 0.9f, 1f);
            button.colors = colors;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = defaultFont;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            var layoutElement = buttonGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = rect.sizeDelta.x;
            layoutElement.preferredHeight = rect.sizeDelta.y;

            var view = buttonGo.AddComponent<CardButtonView>();
            view.Interactable = true;
            return view;
        }

        private void ConfigureCardView(CardButtonView view, CardDefinition card, bool interactable)
        {
            view.Initialize(card, interactable ? OnPlayerCardViewClicked : null);
            view.Interactable = interactable;

            var button = view.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }

            EnsureLayoutElement(view);
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

            if (layout.preferredWidth <= 0f || layout.preferredHeight <= 0f)
            {
                if (view.transform is RectTransform rect)
                {
                    if (layout.preferredWidth <= 0f)
                    {
                        layout.preferredWidth = rect.sizeDelta.x;
                    }

                    if (layout.preferredHeight <= 0f)
                    {
                        layout.preferredHeight = rect.sizeDelta.y;
                    }
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
        }

        private void LoadPrefabIfNeeded(ref CardButtonView prefabField, string resourcePath)
        {
            if (prefabField != null || string.IsNullOrEmpty(resourcePath))
            {
                return;
            }

            var loaded = Resources.Load<GameObject>(resourcePath);
            if (loaded != null)
            {
                prefabField = loaded.GetComponent<CardButtonView>();
            }
        }

        private void OnPlayerCardViewClicked(CardButtonView view)
        {
            OnPlayerCardClicked?.Invoke(view.Card, view);
        }

        private void HandleTargetButtonClicked(Button button, int target)
        {
            HighlightTarget(target);
            OnTargetSelected?.Invoke(target);
        }

        private void ClearTargetButtons()
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
            else if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = gameObject.AddComponent<CanvasScaler>();
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsureDefaultFont()
        {
            if (defaultFont != null)
            {
                return;
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (defaultFont == null)
            {
                var osFonts = Font.GetOSInstalledFontNames();
                if (osFonts != null && osFonts.Length > 0)
                {
                    defaultFont = Font.CreateDynamicFontFromOSFont(osFonts[0], 32);
                }
            }

            if (defaultFont == null)
            {
                Debug.LogError("UI에 사용할 기본 폰트를 찾을 수 없습니다. Canvas에 폰트를 직접 지정해주세요.", this);
            }
        }
    }
}
