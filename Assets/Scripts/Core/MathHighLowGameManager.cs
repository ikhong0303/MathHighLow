using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MathHighLow.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MathHighLow.Core
{
    /// <summary>
    /// 라운드 진행을 담당하는 메인 게임 매니저.
    /// </summary>
    public class MathHighLowGameManager : MonoBehaviour
    {
        [Header("라운드 설정")]
        [SerializeField] private int cardsPerPlayer = 4;
        [SerializeField] private float dealInterval = 0.2f;
        [SerializeField] private int[] targetValues = { 20, 1 };

        [Header("참조")]
        [SerializeField] private GameUIController uiController;

        private readonly List<CardDefinition> playerHand = new();
        private readonly List<CardDefinition> aiHand = new();
        private readonly List<CardDefinition> playerExpression = new();
        private readonly SimpleAiPlayer aiPlayer = new();
        private DeckService deckService;
        private int selectedTarget;
        private bool roundActive;

        private void Awake()
        {
            deckService = new DeckService();
            EnsureUi();
        }

        private void Start()
        {
            StartCoroutine(GameLoop());
        }

        private void EnsureUi()
        {
            if (uiController == null)
            {
                uiController = FindObjectOfType<GameUIController>();
            }

            if (uiController == null)
            {
                var canvasGo = new GameObject("GameCanvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                uiController = canvasGo.AddComponent<GameUIController>();
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }

        }


        private void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnPlayerCardClicked -= HandlePlayerCardClicked;
                uiController.OnTargetSelected -= HandleTargetSelected;
                uiController.OnSubmitRequested -= HandleSubmitRequested;
                uiController.OnResetRequested -= HandleResetRequested;
            }
        }
        private IEnumerator GameLoop()
        {
            uiController.BuildLayout();
            uiController.SetTargetOptions(targetValues);
            uiController.OnPlayerCardClicked += HandlePlayerCardClicked;
            uiController.OnTargetSelected += HandleTargetSelected;
            uiController.OnSubmitRequested += HandleSubmitRequested;
            uiController.OnResetRequested += HandleResetRequested;

            if (targetValues != null && targetValues.Length > 0)
            {
                selectedTarget = targetValues[0];
                uiController.HighlightTarget(selectedTarget);
            }

            while (true)
            {
                yield return StartCoroutine(PlayRound());
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator PlayRound()
        {
            roundActive = false;
            playerExpression.Clear();
            playerHand.Clear();
            aiHand.Clear();
            uiController.PrepareForRound();
            uiController.HighlightTarget(selectedTarget);
            uiController.SetStatusMessage("카드를 드로우합니다...");

            deckService.Shuffle();
            var usedNumberValues = new HashSet<int>();

            for (var i = 0; i < cardsPerPlayer; i++)
            {
                var aiCard = DrawUniqueCard(usedNumberValues);
                aiHand.Add(aiCard);
                uiController.AddAiCard(aiCard);

                yield return new WaitForSeconds(dealInterval);
            }

            for (var i = 0; i < cardsPerPlayer; i++)
            {
                var playerCard = DrawUniqueCard(usedNumberValues);
                playerHand.Add(playerCard);
                uiController.AddPlayerCard(playerCard);

                yield return new WaitForSeconds(dealInterval);
            }

            roundActive = true;
            uiController.SetStatusMessage("카드를 선택해 수식을 만들어주세요.");
            uiController.UpdatePlayerExpression(string.Empty);
            uiController.UpdateAiExpression(string.Empty);

            yield return new WaitUntil(() => !roundActive);
        }

        private void HandlePlayerCardClicked(CardDefinition card, CardButtonView view)
        {
            if (!roundActive)
            {
                return;
            }

            if (!TryAddCardToExpression(card))
            {
                return;
            }

            view.Interactable = false;
            uiController.MovePlayerCardToExpression(view);
            UpdatePlayerExpressionView();
        }

        private void HandleTargetSelected(int targetValue)
        {
            selectedTarget = targetValue;
            uiController.HighlightTarget(selectedTarget);
        }

        private void HandleResetRequested()
        {
            if (!roundActive)
            {
                return;
            }

            playerExpression.Clear();
            uiController.ResetPlayerCards();
            foreach (var card in playerHand)
            {
                uiController.AddPlayerCard(card);
            }

            uiController.UpdatePlayerExpression(string.Empty);
            uiController.SetStatusMessage("수식을 다시 만들어주세요.");
        }

        private void HandleSubmitRequested()
        {
            if (!roundActive)
            {
                return;
            }

            if (!MathExpressionEvaluator.TryEvaluate(playerExpression, out var playerResult, out var playerExpressionText, out var error))
            {
                uiController.SetStatusMessage(error);
                return;
            }

            var aiExpressionCards = aiPlayer.BuildExpression(aiHand, selectedTarget);
            MathExpressionEvaluator.TryEvaluate(aiExpressionCards, out var aiResult, out var aiExpressionText, out _);

            uiController.UpdatePlayerExpression(playerExpressionText + " = " + playerResult.ToString("0.##"));
            uiController.UpdateAiExpression(aiExpressionText.Length > 0 ? aiExpressionText + " = " + aiResult.ToString("0.##") : "AI가 유효한 수식을 만들지 못했습니다.");

            var outcome = DetermineOutcome(playerResult, aiResult, aiExpressionCards.Count > 0);
            uiController.SetStatusMessage(outcome);
            roundActive = false;
        }

        private bool TryAddCardToExpression(CardDefinition card)
        {
            if (card == null)
            {
                return false;
            }

            if (playerExpression.Count == 0)
            {
                if (card.Kind != CardKind.Number)
                {
                    uiController.SetStatusMessage("첫 카드는 숫자여야 합니다.");
                    return false;
                }
            }
            else
            {
                var lastCard = playerExpression[^1];
                if (lastCard.Kind == card.Kind)
                {
                    uiController.SetStatusMessage("숫자와 연산자를 번갈아 선택해야 합니다.");
                    return false;
                }
            }

            if (card.Kind == CardKind.Operator && !playerExpression.Any())
            {
                return false;
            }

            playerExpression.Add(card);
            return true;
        }

        private void UpdatePlayerExpressionView()
        {
            if (!MathExpressionEvaluator.TryBuildExpressionString(playerExpression, out var expression, out _))
            {
                uiController.UpdatePlayerExpression(string.Empty);
                return;
            }

            uiController.UpdatePlayerExpression(expression);
        }

        private string DetermineOutcome(double playerResult, double aiResult, bool aiHasExpression)
        {
            var playerDistance = Mathf.Abs((float)(playerResult - selectedTarget));
            var aiDistance = aiHasExpression ? Mathf.Abs((float)(aiResult - selectedTarget)) : float.PositiveInfinity;

            if (playerDistance < aiDistance)
            {
                return $"플레이어 승리! 목표값 {selectedTarget}에 더 가깝습니다.";
            }

            if (Mathf.Approximately(playerDistance, aiDistance))
            {
                return "무승부입니다.";
            }

            return "AI 승리! 더 목표에 근접했습니다.";
        }

        private CardDefinition DrawUniqueCard(HashSet<int> usedNumberValues)
        {
            CardDefinition lastDrawn = null;

            for (var attempt = 0; attempt < 256; attempt++)
            {
                var card = deckService.Draw();
                lastDrawn = card;

                if (card.Kind != CardKind.Number)
                {
                    return card;
                }

                if (usedNumberValues.Add(card.NumberValue))
                {
                    return card;
                }
            }

            return lastDrawn ?? deckService.Draw();
        }
    }
}
