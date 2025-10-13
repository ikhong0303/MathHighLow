using System;
using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Core
{
    /// <summary>
    /// 기본 규칙을 만족하는 간단한 AI 전략.
    /// 두 숫자와 하나의 연산자, 혹은 세 숫자와 두 연산자를 조합합니다.
    /// </summary>
    public class SimpleAiPlayer
    {
        private readonly List<CardDefinition> workingBuffer = new();
        private readonly List<CardDefinition> bestExpression = new();

        public IReadOnlyList<CardDefinition> BuildExpression(IReadOnlyList<CardDefinition> hand, int target)
        {
            bestExpression.Clear();

            if (hand == null || hand.Count == 0)
            {
                return Array.Empty<CardDefinition>();
            }

            var numbers = hand.Where(card => card.Kind == CardKind.Number).ToList();
            var operators = hand.Where(card => card.Kind == CardKind.Operator).ToList();

            if (numbers.Count == 0)
            {
                return Array.Empty<CardDefinition>();
            }

            EvaluateTwoNumberExpressions(numbers, operators, target);
            EvaluateThreeNumberExpressions(numbers, operators, target);

            if (bestExpression.Count == 0)
            {
                bestExpression.Add(numbers[0].Clone());
            }

            return bestExpression.Select(card => card.Clone()).ToList();
        }

        private void EvaluateTwoNumberExpressions(List<CardDefinition> numbers, List<CardDefinition> operators, int target)
        {
            foreach (var first in numbers)
            {
                foreach (var second in numbers)
                {
                    if (ReferenceEquals(first, second))
                    {
                        continue;
                    }

                    foreach (var op in operators)
                    {
                        workingBuffer.Clear();
                        workingBuffer.Add(first);
                        workingBuffer.Add(op);
                        workingBuffer.Add(second);

                        ScoreCurrentWorkingBuffer(target);
                    }
                }
            }
        }

        private void EvaluateThreeNumberExpressions(List<CardDefinition> numbers, List<CardDefinition> operators, int target)
        {
            if (numbers.Count < 3 || operators.Count < 2)
            {
                return;
            }

            foreach (var first in numbers)
            {
                foreach (var second in numbers)
                {
                    if (ReferenceEquals(first, second))
                    {
                        continue;
                    }

                    foreach (var third in numbers)
                    {
                        if (ReferenceEquals(first, third) || ReferenceEquals(second, third))
                        {
                            continue;
                        }

                        foreach (var firstOp in operators)
                        {
                            foreach (var secondOp in operators)
                            {
                                if (ReferenceEquals(firstOp, secondOp) && operators.Count < 2)
                                {
                                    continue;
                                }

                                workingBuffer.Clear();
                                workingBuffer.Add(first);
                                workingBuffer.Add(firstOp);
                                workingBuffer.Add(second);
                                workingBuffer.Add(secondOp);
                                workingBuffer.Add(third);

                                ScoreCurrentWorkingBuffer(target);
                            }
                        }
                    }
                }
            }
        }

        private void ScoreCurrentWorkingBuffer(int target)
        {
            if (!MathExpressionEvaluator.TryEvaluate(workingBuffer, out var result, out _, out _))
            {
                return;
            }

            if (bestExpression.Count == 0)
            {
                CopyWorkingBuffer();
                return;
            }

            var currentDistance = Math.Abs(result - target);
            if (!MathExpressionEvaluator.TryEvaluate(bestExpression, out var bestResult, out _, out _))
            {
                CopyWorkingBuffer();
                return;
            }

            var bestDistance = Math.Abs(bestResult - target);
            if (currentDistance < bestDistance)
            {
                CopyWorkingBuffer();
            }
        }

        private void CopyWorkingBuffer()
        {
            bestExpression.Clear();
            foreach (var card in workingBuffer)
            {
                bestExpression.Add(card.Clone());
            }
        }
    }
}
