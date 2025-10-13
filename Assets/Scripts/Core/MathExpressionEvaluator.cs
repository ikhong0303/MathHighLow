using System;
using System.Collections.Generic;
using System.Text;

namespace MathHighLow.Core
{
    /// <summary>
    /// 카드 목록을 바탕으로 수식을 계산하고 검증합니다.
    /// </summary>
    public static class MathExpressionEvaluator
    {
        public static bool TryBuildExpressionString(IReadOnlyList<CardDefinition> cards, out string expression, out string error)
        {
            expression = string.Empty;
            error = string.Empty;

            if (cards == null || cards.Count == 0)
            {
                error = "카드를 선택해 수식을 만들어주세요.";
                return false;
            }

            var stringBuilder = new StringBuilder();
            var expectNumber = true;

            foreach (var card in cards)
            {
                if (expectNumber)
                {
                    if (card.Kind != CardKind.Number)
                    {
                        error = "수식은 숫자로 시작해야 합니다.";
                        return false;
                    }

                    stringBuilder.Append(card.NumberValue);
                    expectNumber = false;
                }
                else
                {
                    if (card.Kind != CardKind.Operator)
                    {
                        error = "연산자와 숫자가 번갈아 나와야 합니다.";
                        return false;
                    }

                    stringBuilder.Append(' ');
                    stringBuilder.Append(card.OperatorValue.ToSymbol());
                    stringBuilder.Append(' ');
                    expectNumber = true;
                }
            }

            if (expectNumber)
            {
                error = "수식이 연산자로 끝날 수 없습니다.";
                return false;
            }

            expression = stringBuilder.ToString();
            return true;
        }

        public static bool TryEvaluate(IReadOnlyList<CardDefinition> cards, out double result, out string expression, out string error)
        {
            result = 0;
            expression = string.Empty;
            error = string.Empty;

            if (!TryBuildExpressionString(cards, out expression, out error))
            {
                return false;
            }

            var values = new Stack<double>();
            var operators = new Stack<OperatorType>();

            try
            {
                foreach (var card in cards)
                {
                    if (card.Kind == CardKind.Number)
                    {
                        values.Push(card.NumberValue);
                    }
                    else
                    {
                        while (operators.Count > 0 && HasHigherOrEqualPrecedence(operators.Peek(), card.OperatorValue))
                        {
                            if (!EvaluateStep(values, operators.Pop(), out var stepResult))
                            {
                                error = "연산 중 오류가 발생했습니다.";
                                return false;
                            }

                            values.Push(stepResult);
                        }

                        operators.Push(card.OperatorValue);
                    }
                }

                while (operators.Count > 0)
                {
                    if (!EvaluateStep(values, operators.Pop(), out var stepResult))
                    {
                        error = "연산 중 오류가 발생했습니다.";
                        return false;
                    }

                    values.Push(stepResult);
                }

                if (values.Count != 1)
                {
                    error = "수식 계산 결과가 잘못되었습니다.";
                    return false;
                }

                result = values.Pop();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool EvaluateStep(Stack<double> values, OperatorType operatorType, out double stepResult)
        {
            stepResult = 0;

            if (values.Count < 2)
            {
                return false;
            }

            var right = values.Pop();
            var left = values.Pop();

            if (operatorType == OperatorType.Divide && Math.Abs(right) < double.Epsilon)
            {
                return false;
            }

            stepResult = operatorType.Apply(left, right);
            return true;
        }

        private static bool HasHigherOrEqualPrecedence(OperatorType first, OperatorType second)
        {
            return GetPriority(first) >= GetPriority(second);
        }

        private static int GetPriority(OperatorType operatorType)
        {
            return operatorType switch
            {
                OperatorType.Multiply => 2,
                OperatorType.Divide => 2,
                OperatorType.Add => 1,
                OperatorType.Subtract => 1,
                _ => 0
            };
        }
    }
}
