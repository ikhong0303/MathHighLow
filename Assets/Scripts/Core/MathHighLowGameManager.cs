using System;
using System.Collections;
using System.Collections.Generic;
using MathHighLow.UI;
using UnityEngine;

namespace MathHighLow.Core
{
    /// <summary>
    /// 코덱스 지시서에 맞춰 라운드를 상태 머신으로 제어하는 메인 매니저.
    /// </summary>
    public class MathHighLowGameManager : MonoBehaviour
    {
        private enum RoundPhase
        {
            Dealing,
            Waiting,
            Evaluating,
            Results
        }

        [Header("라운드 구성")]
        [SerializeField] private float dealInterval = 0.2f;
        [SerializeField] private int initialSlots = 3;
        [SerializeField] private float submissionUnlockTime = 30f;
        [SerializeField] private float roundDuration = 180f;
        [SerializeField] private int startingCredits = 20;
        [SerializeField] private int minBet = 1;
        [SerializeField] private int maxBet = 5;
        [SerializeField] private int[] targetValues = { 1, 20 };

        [Header("연출")]
        [SerializeField, Min(0f)] private float resultsDisplayDuration = 10f;

        [Header("참조")]
        [SerializeField] private GameUIController uiController;

        private readonly List<PlayerCardState> playerCards = new();
        private readonly List<ExpressionToken> playerTokens = new();
        private readonly DeckService deckService = new();
        private readonly RoundHandState handState = new();
        private readonly AiSolver aiSolver = new();

        private RoundPhase phase;
        private float elapsed;
        private bool submitted;
        private int selectedTarget;
        private int currentBet;
        private int playerCredits;
        private int aiCredits;
        private int multiplyUsed;
        private int sqrtUsed;
        private bool expectNumber;
        private RoundHandSnapshot handSnapshot;

        private void Awake()
        {
            if (uiController == null)
            {
                uiController = FindObjectOfType<GameUIController>();
            }

            if (uiController == null)
            {
                Debug.LogError("GameUIController를 찾을 수 없습니다.", this);
            }
        }

        private void Start()
        {
            playerCredits = startingCredits;
            aiCredits = startingCredits;

            EnsureUi();
            StartCoroutine(GameLoop());
        }

        private void OnDestroy()
        {
            if (uiController == null)
            {
                return;
            }

            uiController.OnNumberCardClicked -= HandleNumberCardClicked;
            uiController.OnOperatorSelected -= HandleOperatorSelected;
            uiController.OnSqrtSelected -= HandleSqrtSelected;
            uiController.OnSubmitRequested -= HandleSubmitRequested;
            uiController.OnResetRequested -= HandleResetRequested;
            uiController.OnTargetSelected -= HandleTargetSelected;
            uiController.OnBetIncreaseRequested -= HandleBetIncreaseRequested;
            uiController.OnBetDecreaseRequested -= HandleBetDecreaseRequested;
        }

        private void EnsureUi()
        {
            if (uiController == null)
            {
                return;
            }

            uiController.BuildLayout();
            uiController.OnNumberCardClicked += HandleNumberCardClicked;
            uiController.OnOperatorSelected += HandleOperatorSelected;
            uiController.OnSqrtSelected += HandleSqrtSelected;
            uiController.OnSubmitRequested += HandleSubmitRequested;
            uiController.OnResetRequested += HandleResetRequested;
            uiController.OnTargetSelected += HandleTargetSelected;
            uiController.OnBetIncreaseRequested += HandleBetIncreaseRequested;
            uiController.OnBetDecreaseRequested += HandleBetDecreaseRequested;

            uiController.SetTargetOptions(targetValues);
            if (targetValues != null && targetValues.Length > 0)
            {
                selectedTarget = targetValues[0];
                uiController.HighlightTarget(selectedTarget);
            }

            currentBet = minBet;
            uiController.UpdateBetDisplay(currentBet);
            uiController.UpdateCredits(playerCredits, aiCredits);
            uiController.SetStatusMessage("게임을 준비하는 중...");
        }

        private IEnumerator GameLoop()
        {
            while (true)
            {
                yield return PlayRound();
            }
        }

        private IEnumerator PlayRound()
        {
            phase = RoundPhase.Dealing;
            submitted = false;
            multiplyUsed = 0;
            sqrtUsed = 0;
            expectNumber = true;
            elapsed = 0f;
            playerTokens.Clear();
            playerCards.Clear();
            handState.Reset();
            deckService.BuildSlotDeck();

            uiController.PrepareForRound();
            uiController.UpdateCredits(playerCredits, aiCredits);
            uiController.UpdateBetDisplay(currentBet);
            uiController.SetStatusMessage("카드를 분배합니다...");
            uiController.SetSubmitInteractable(false, "30초 이전");
            uiController.UpdateTimer(0f, roundDuration, submissionUnlockTime);
            uiController.UpdateSpecialBadges(0, 0);

            for (var i = 0; i < initialSlots; i++)
            {
                var slotCard = deckService.DrawSlotCard();
                yield return HandleDealtCard(slotCard);
                yield return new WaitForSeconds(dealInterval);
            }

            handSnapshot = handState.CreateSnapshot();
            uiController.UpdateSpecialBadges(handState.MultiplyCount - multiplyUsed, handState.SquareRootCount - sqrtUsed);
            uiController.SetStatusMessage("수식을 만들어 제출하세요.");

            phase = RoundPhase.Waiting;
            elapsed = 0f;
            expectNumber = true;
            while (!submitted)
            {
                elapsed += Time.deltaTime;
                var windowOpen = elapsed >= submissionUnlockTime;
                uiController.UpdateTimer(elapsed, roundDuration, submissionUnlockTime);

                EvaluateSubmissionEligibility(windowOpen);

                if (elapsed >= roundDuration)
                {
                    HandleSubmit();
                }

                if (submitted)
                {
                    break;
                }

                yield return null;
            }

            phase = RoundPhase.Evaluating;

            var playerResult = EvaluatePlayerExpression(out var playerExpressionText, out var validationError);
            var aiTokens = aiSolver.FindBestExpression(handSnapshot, selectedTarget);
            var aiValidation = ExpressionValidator.Validate(handSnapshot, aiTokens);

            var playerExpressionDisplay = playerExpressionText;
            var playerError = string.Empty;
            double? playerValue = null;
            double? playerDifference = null;

            if (string.IsNullOrEmpty(playerExpressionDisplay))
            {
                playerError = string.IsNullOrEmpty(validationError)
                    ? "수식이 유효하지 않습니다."
                    : validationError;
            }
            else if (!double.IsFinite(playerResult))
            {
                playerError = string.IsNullOrEmpty(validationError)
                    ? "결과 계산 실패"
                    : validationError;
            }
            else
            {
                playerValue = playerResult;
                playerDifference = Math.Abs(playerResult - selectedTarget);
            }

            uiController.ShowPlayerOutcome(selectedTarget, playerExpressionDisplay, playerValue, playerDifference, playerError);

            var aiExpressionDisplay = aiValidation.IsValid ? aiValidation.ExpressionText : string.Empty;
            var aiError = aiValidation.IsValid ? string.Empty : "유효한 수식을 찾지 못했습니다.";
            double? aiValue = aiValidation.IsValid ? aiValidation.Result : null;
            double? aiDifference = aiValidation.IsValid ? Math.Abs(aiValidation.Result - selectedTarget) : null;

            uiController.ShowAiOutcome(selectedTarget, aiExpressionDisplay, aiValue, aiDifference, aiError);

            var playerValid = double.IsFinite(playerResult);
            var aiValid = aiValidation.IsValid;
            var playerDistance = playerValid ? Math.Abs(playerResult - selectedTarget) : double.PositiveInfinity;
            var aiDistance = aiValid ? Math.Abs(aiValidation.Result - selectedTarget) : double.PositiveInfinity;

            var outcome = DetermineOutcome(playerDistance, aiDistance);
            uiController.ShowRoundResult(outcome.summary, outcome.detail);

            playerCredits = outcome.playerCredits;
            aiCredits = outcome.aiCredits;
            uiController.UpdateCredits(playerCredits, aiCredits);

            phase = RoundPhase.Results;
            yield return new WaitForSeconds(Mathf.Max(0f, resultsDisplayDuration));
        }

        private void EvaluateSubmissionEligibility(bool windowOpen)
        {
            var snapshot = handSnapshot;
            var validation = ExpressionValidator.Validate(snapshot, playerTokens);

            if (!windowOpen)
            {
                uiController.SetSubmitInteractable(false, "30초 이전");
                return;
            }

            if (!validation.IsValid)
            {
                uiController.SetSubmitInteractable(false, validation.Error);
                return;
            }

            if (selectedTarget == 0)
            {
                uiController.SetSubmitInteractable(false, "목표 미선택");
                return;
            }

            if (currentBet < minBet || currentBet > maxBet)
            {
                uiController.SetSubmitInteractable(false, "베팅 미설정");
                return;
            }

            uiController.SetSubmitInteractable(true, string.Empty);
        }

        private double EvaluatePlayerExpression(out string formattedExpression, out string error)
        {
            formattedExpression = string.Empty;
            error = string.Empty;

            var validation = ExpressionValidator.Validate(handSnapshot, playerTokens);
            if (!validation.IsValid)
            {
                error = validation.Error;
                return double.PositiveInfinity;
            }

            formattedExpression = validation.ExpressionText;
            return validation.Result;
        }

        private (string summary, string detail, int playerCredits, int aiCredits) DetermineOutcome(double playerDistance, double aiDistance)
        {
            var betInfo = $"목표 ={selectedTarget}, 베팅 ${currentBet}";

            if (double.IsInfinity(playerDistance) && double.IsInfinity(aiDistance))
            {
                var balanceInfo = $"새 잔액 | 플레이어 ${playerCredits}, AI ${aiCredits}";
                var detailText = $"{betInfo}\n양측 모두 유효한 수식을 만들지 못했습니다.\n{balanceInfo}";
                return ("라운드 무효", detailText, playerCredits, aiCredits);
            }

            var distanceInfo = $"차이 | 플레이어 {playerDistance:0.##}, AI {aiDistance:0.##}";

            if (Mathf.Approximately((float)playerDistance, (float)aiDistance))
            {
                var balanceInfo = $"새 잔액 | 플레이어 ${playerCredits}, AI ${aiCredits}";
                var detailText = $"{betInfo}\n{distanceInfo}\n{balanceInfo}";
                return ("무승부", detailText, playerCredits, aiCredits);
            }

            if (playerDistance < aiDistance)
            {
                var newPlayerCredits = playerCredits + currentBet;
                var newAiCredits = aiCredits - currentBet;
                var balanceInfo = $"새 잔액 | 플레이어 ${newPlayerCredits}, AI ${newAiCredits}";
                var detailText = $"{betInfo}\n{distanceInfo}\n{balanceInfo}";
                return ("플레이어 승리", detailText, newPlayerCredits, newAiCredits);
            }

            var updatedPlayerCredits = playerCredits - currentBet;
            var updatedAiCredits = aiCredits + currentBet;
            var updatedBalanceInfo = $"새 잔액 | 플레이어 ${updatedPlayerCredits}, AI ${updatedAiCredits}";
            var detail = $"{betInfo}\n{distanceInfo}\n{updatedBalanceInfo}";
            return ("AI 승리", detail, updatedPlayerCredits, updatedAiCredits);
        }

        private IEnumerator HandleDealtCard(CardDefinition card)
        {
            if (card.Kind == CardKind.Number)
            {
                AddNumberCard(card.NumberValue);
                yield break;
            }

            if (card.Kind == CardKind.Special)
            {
                handState.AddSpecialCard(card.SpecialType);
                uiController.UpdateSpecialBadges(Mathf.Max(0, handState.MultiplyCount - multiplyUsed), Mathf.Max(0, handState.SquareRootCount - sqrtUsed));

                switch (card.SpecialType)
                {
                    case SpecialCardType.SquareRoot:
                        var sqrtNumber = deckService.DrawNumberCard();
                        AddNumberCard(sqrtNumber.NumberValue);
                        break;
                    case SpecialCardType.Multiply:
                        var multiplyNumber = deckService.DrawNumberCard();
                        AddNumberCard(multiplyNumber.NumberValue);
                        yield return PromptDisableOperator();
                        break;
                }
            }
        }

        private IEnumerator PromptDisableOperator()
        {
            var available = new List<OperatorType>
            {
                OperatorType.Add,
                OperatorType.Subtract,
                OperatorType.Divide
            };

            available.RemoveAll(op => !handState.IsOperatorEnabled(op));

            if (available.Count == 0)
            {
                yield break;
            }

            var selectionComplete = false;
            uiController.ShowDisableOperatorPrompt(available, selected =>
            {
                handState.DisableBaseOperator(selected);
                uiController.SetOperatorEnabled(selected, false);
                selectionComplete = true;
            });

            yield return new WaitUntil(() => selectionComplete);
            uiController.HideDisableOperatorPrompt();
        }

        private void AddNumberCard(int value)
        {
            handState.AddNumberCard(value);
            var card = new CardDefinition(CardKind.Number, value, OperatorType.Add);
            var view = uiController.AddPlayerNumberCard(card);
            playerCards.Add(new PlayerCardState(card, view));
            uiController.AddAiCard(card);
        }

        private void HandleNumberCardClicked(CardDefinition card, CardButtonView view)
        {
            if (phase != RoundPhase.Waiting || !expectNumber)
            {
                return;
            }

            var state = playerCards.Find(entry => entry.View == view);
            if (state == null || state.Used)
            {
                return;
            }

            state.Used = true;
            view.Interactable = false;
            playerTokens.Add(ExpressionToken.NumberToken(card.NumberValue));
            expectNumber = false;
            UpdateExpressionPreview();
        }

        private void HandleOperatorSelected(OperatorType operatorType)
        {
            if (phase != RoundPhase.Waiting || expectNumber)
            {
                return;
            }

            if (operatorType == OperatorType.Multiply)
            {
                if (multiplyUsed >= handState.MultiplyCount)
                {
                    return;
                }

                multiplyUsed++;
            }
            else if (!handState.IsOperatorEnabled(operatorType))
            {
                return;
            }

            playerTokens.Add(ExpressionToken.BinaryOperatorToken(operatorType));
            expectNumber = true;
            uiController.UpdateSpecialBadges(Mathf.Max(0, handState.MultiplyCount - multiplyUsed), Mathf.Max(0, handState.SquareRootCount - sqrtUsed));
            UpdateExpressionPreview();
        }

        private void HandleSqrtSelected()
        {
            if (phase != RoundPhase.Waiting || !expectNumber)
            {
                return;
            }

            if (sqrtUsed >= handState.SquareRootCount)
            {
                return;
            }

            sqrtUsed++;
            playerTokens.Add(ExpressionToken.UnaryOperatorToken(SpecialCardType.SquareRoot));
            uiController.UpdateSpecialBadges(Mathf.Max(0, handState.MultiplyCount - multiplyUsed), Mathf.Max(0, handState.SquareRootCount - sqrtUsed));
            UpdateExpressionPreview();
        }

        private void HandleResetRequested()
        {
            if (phase != RoundPhase.Waiting)
            {
                return;
            }

            ResetExpression();
        }

        private void ResetExpression()
        {
            foreach (var state in playerCards)
            {
                state.Used = false;
                if (state.View != null)
                {
                    state.View.Interactable = true;
                }
            }

            playerTokens.Clear();
            multiplyUsed = 0;
            sqrtUsed = 0;
            expectNumber = true;
            uiController.UpdateSpecialBadges(Mathf.Max(0, handState.MultiplyCount - multiplyUsed), Mathf.Max(0, handState.SquareRootCount - sqrtUsed));
            UpdateExpressionPreview();
        }

        private void HandleTargetSelected(int target)
        {
            selectedTarget = target;
            uiController.HighlightTarget(selectedTarget);
        }

        private void HandleBetIncreaseRequested()
        {
            currentBet = Mathf.Clamp(currentBet + 1, minBet, maxBet);
            uiController.UpdateBetDisplay(currentBet);
        }

        private void HandleBetDecreaseRequested()
        {
            currentBet = Mathf.Clamp(currentBet - 1, minBet, maxBet);
            uiController.UpdateBetDisplay(currentBet);
        }

        private void HandleSubmitRequested()
        {
            if (phase != RoundPhase.Waiting)
            {
                return;
            }

            if (elapsed < submissionUnlockTime)
            {
                return;
            }

            HandleSubmit();
        }

        private void HandleSubmit()
        {
            submitted = true;
            uiController.SetSubmitInteractable(false, string.Empty);
        }

        private void UpdateExpressionPreview()
        {
            var preview = BuildExpressionPreview();
            uiController.UpdatePlayerExpression(preview);
            EvaluateSubmissionEligibility(elapsed >= submissionUnlockTime);
        }

        private string BuildExpressionPreview()
        {
            if (playerTokens.Count == 0)
            {
                return "수식을 구성 중...";
            }

            var parts = new List<string>();
            foreach (var token in playerTokens)
            {
                switch (token.Type)
                {
                    case ExpressionTokenType.Number:
                        parts.Add(token.Number.ToString("0"));
                        break;
                    case ExpressionTokenType.BinaryOperator:
                        parts.Add(token.BinaryOperator.ToSymbol());
                        break;
                    case ExpressionTokenType.UnaryOperator:
                        parts.Add("√");
                        break;
                }
            }

            return string.Join(" ", parts);
        }

        private sealed class PlayerCardState
        {
            public PlayerCardState(CardDefinition card, CardButtonView view)
            {
                Card = card;
                View = view;
            }

            public CardDefinition Card { get; }

            public CardButtonView View { get; }

            public bool Used { get; set; }
        }
    }
}
