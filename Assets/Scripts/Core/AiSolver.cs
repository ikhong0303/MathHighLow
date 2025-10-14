using System;
using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Core
{
    /// <summary>
    /// 솔로 플레이 시 AI의 최적 수식을 탐색합니다.
    /// </summary>
    public class AiSolver
    {
        private readonly List<ExpressionToken> workingTokens = new();
        private readonly List<ExpressionToken> bestTokens = new();
        private readonly Dictionary<int, int> numberUsage = new();
        private readonly List<OperatorType> baseOperators = new();
        private double bestDistance = double.PositiveInfinity;

        public IReadOnlyList<ExpressionToken> FindBestExpression(RoundHandSnapshot hand, int target)
        {
            bestTokens.Clear();
            numberUsage.Clear();
            baseOperators.Clear();

            foreach (var number in hand.Numbers)
            {
                numberUsage.TryGetValue(number, out var count);
                numberUsage[number] = count + 1;
            }

            if (hand.Numbers.Count == 0)
            {
                return Array.Empty<ExpressionToken>();
            }

            baseOperators.AddRange(hand.GetEnabledBaseOperators());

            var remainingBaseSlots = hand.Numbers.Count - 1 - hand.MultiplyCount;
            if (remainingBaseSlots < 0)
            {
                return Array.Empty<ExpressionToken>();
            }

            if (remainingBaseSlots > 0 && baseOperators.Count == 0)
            {
                return Array.Empty<ExpressionToken>();
            }

            bestDistance = double.PositiveInfinity;

            PermuteNumbers(hand, target);

            return bestTokens.Count == 0
                ? Array.Empty<ExpressionToken>()
                : bestTokens.ToArray();
        }

        private void PermuteNumbers(RoundHandSnapshot hand, int target)
        {
            var counts = new Dictionary<int, int>(numberUsage);
            BacktrackNumbers(hand, target, counts, 0, new int[hand.Numbers.Count]);
        }

        private void BacktrackNumbers(RoundHandSnapshot hand, int target, Dictionary<int, int> counts, int index, int[] ordered)
        {
            if (index == ordered.Length)
            {
                EvaluateWithOperators(hand, target, ordered);
                return;
            }

            foreach (var kvp in counts.ToArray())
            {
                if (kvp.Value <= 0)
                {
                    continue;
                }

                counts[kvp.Key] = kvp.Value - 1;
                ordered[index] = kvp.Key;
                BacktrackNumbers(hand, target, counts, index + 1, ordered);
                counts[kvp.Key] = kvp.Value;
            }
        }

        private void EvaluateWithOperators(RoundHandSnapshot hand, int target, int[] orderedNumbers)
        {
            var sqrtDistribution = new int[orderedNumbers.Length];
            DistributeSquareRoots(hand.SquareRootCount, 0, orderedNumbers, sqrtDistribution, hand, target);
        }

        private void DistributeSquareRoots(int remaining, int index, int[] numbers, int[] distribution, RoundHandSnapshot hand, int target)
        {
            if (index == numbers.Length)
            {
                if (remaining == 0)
                {
                    EnumerateOperators(hand, numbers, distribution, target);
                }

                return;
            }

            for (var count = 0; count <= remaining; count++)
            {
                distribution[index] = count;
                DistributeSquareRoots(remaining - count, index + 1, numbers, distribution, hand, target);
            }
        }

        private void EnumerateOperators(RoundHandSnapshot hand, int[] numbers, int[] sqrtDistribution, int target)
        {
            var totalSlots = numbers.Length - 1;
            var operators = new OperatorType[totalSlots];
            AssignOperators(hand, numbers, sqrtDistribution, operators, 0, 0, target);
        }

        private void AssignOperators(RoundHandSnapshot hand, int[] numbers, int[] sqrtDistribution, OperatorType[] operators, int index, int multiplyUsed, int target)
        {
            var slotsRemaining = operators.Length - index;
            var remainingMultiplyNeeded = hand.MultiplyCount - multiplyUsed;

            if (remainingMultiplyNeeded > slotsRemaining)
            {
                return;
            }

            if (index == operators.Length)
            {
                if (multiplyUsed == hand.MultiplyCount)
                {
                    BuildAndScore(hand, numbers, sqrtDistribution, operators, target);
                }

                return;
            }

            if (multiplyUsed < hand.MultiplyCount)
            {
                operators[index] = OperatorType.Multiply;
                AssignOperators(hand, numbers, sqrtDistribution, operators, index + 1, multiplyUsed + 1, target);
            }

            foreach (var baseOp in baseOperators)
            {
                operators[index] = baseOp;
                AssignOperators(hand, numbers, sqrtDistribution, operators, index + 1, multiplyUsed, target);
            }
        }

        private void BuildAndScore(RoundHandSnapshot hand, int[] numbers, int[] sqrtDistribution, OperatorType[] operators, int target)
        {
            workingTokens.Clear();

            for (var i = 0; i < numbers.Length; i++)
            {
                for (var s = 0; s < sqrtDistribution[i]; s++)
                {
                    workingTokens.Add(ExpressionToken.UnaryOperatorToken(SpecialCardType.SquareRoot));
                }

                workingTokens.Add(ExpressionToken.NumberToken(numbers[i]));

                if (i < operators.Length)
                {
                    workingTokens.Add(ExpressionToken.BinaryOperatorToken(operators[i]));
                }
            }

            var validation = ExpressionValidator.Validate(hand, workingTokens);
            if (!validation.IsValid)
            {
                return;
            }

            var distance = Math.Abs(validation.Result - target);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTokens.Clear();
                bestTokens.AddRange(validation.Tokens);
            }
        }
    }
}
