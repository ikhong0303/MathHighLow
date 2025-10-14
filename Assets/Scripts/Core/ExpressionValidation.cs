using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MathHighLow.Core
{
    public enum ExpressionTokenType
    {
        Number,
        BinaryOperator,
        UnaryOperator
    }

    public readonly struct ExpressionToken
    {
        public ExpressionToken(ExpressionTokenType type, double number, OperatorType binaryOperator, SpecialCardType unaryOperator)
        {
            Type = type;
            Number = number;
            BinaryOperator = binaryOperator;
            UnaryOperator = unaryOperator;
        }

        public ExpressionTokenType Type { get; }

        public double Number { get; }

        public OperatorType BinaryOperator { get; }

        public SpecialCardType UnaryOperator { get; }

        public static ExpressionToken NumberToken(int value)
        {
            return new ExpressionToken(ExpressionTokenType.Number, value, OperatorType.Add, SpecialCardType.SquareRoot);
        }

        public static ExpressionToken BinaryOperatorToken(OperatorType operatorType)
        {
            return new ExpressionToken(ExpressionTokenType.BinaryOperator, 0, operatorType, SpecialCardType.SquareRoot);
        }

        public static ExpressionToken UnaryOperatorToken(SpecialCardType specialType)
        {
            return new ExpressionToken(ExpressionTokenType.UnaryOperator, 0, OperatorType.Add, specialType);
        }
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }

        public string Error { get; init; } = string.Empty;

        public double Result { get; init; }

        public string ExpressionText { get; init; } = string.Empty;

        public IReadOnlyList<ExpressionToken> Tokens { get; init; } = Array.Empty<ExpressionToken>();
    }

    public static class ExpressionValidator
    {
        public static ValidationResult Validate(RoundHandSnapshot hand, IReadOnlyList<ExpressionToken> tokens)
        {
            if (hand.Numbers.Count == 0)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "숫자 미사용 0장",
                    Tokens = tokens ?? Array.Empty<ExpressionToken>()
                };
            }

            if (tokens == null || tokens.Count == 0)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = $"숫자 미사용 {hand.Numbers.Count}장",
                    Tokens = Array.Empty<ExpressionToken>()
                };
            }

            var numberUsage = new Dictionary<int, int>();
            foreach (var number in hand.Numbers)
            {
                numberUsage.TryGetValue(number, out var count);
                numberUsage[number] = count + 1;
            }

            var numbersUsed = 0;
            var binaryUsed = 0;
            var sqrtUsed = 0;
            var multiplyUsed = 0;
            var expectNumber = true;
            var invalidNumberUsage = false;

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case ExpressionTokenType.UnaryOperator:
                        if (!expectNumber)
                        {
                            return Invalid("토큰 시퀀스 오류", tokens);
                        }

                        if (token.UnaryOperator != SpecialCardType.SquareRoot)
                        {
                            return Invalid("지원하지 않는 단항 연산자", tokens);
                        }

                        sqrtUsed++;
                        break;

                    case ExpressionTokenType.Number:
                        if (!expectNumber)
                        {
                            return Invalid("토큰 시퀀스 오류", tokens);
                        }

                        numbersUsed++;
                        expectNumber = false;

                        var tokenValue = (int)token.Number;
                        if (!numberUsage.TryGetValue(tokenValue, out var count) || count <= 0)
                        {
                            invalidNumberUsage = true;
                        }
                        else
                        {
                            numberUsage[tokenValue] = count - 1;
                        }

                        break;

                    case ExpressionTokenType.BinaryOperator:
                        if (expectNumber)
                        {
                            return Invalid("토큰 시퀀스 오류", tokens);
                        }

                        if (token.BinaryOperator == OperatorType.Multiply)
                        {
                            multiplyUsed++;
                        }
                        else if (hand.DisabledBaseOperators.Contains(token.BinaryOperator))
                        {
                            return Invalid("비활성화된 연산자 사용", tokens);
                        }

                        binaryUsed++;
                        expectNumber = true;
                        break;
                    default:
                        return Invalid("알 수 없는 토큰", tokens);
                }
            }

            if (expectNumber)
            {
                return Invalid("토큰 시퀀스 오류", tokens);
            }

            if (invalidNumberUsage)
            {
                return Invalid("보유하지 않은 숫자를 사용했습니다.", tokens);
            }

            var remainingNumbers = numberUsage.Values.Where(count => count > 0).Sum();
            if (remainingNumbers > 0)
            {
                return Invalid($"숫자 미사용 {remainingNumbers}장", tokens);
            }

            if (sqrtUsed != hand.SquareRootCount)
            {
                var diff = hand.SquareRootCount - sqrtUsed;
                if (diff > 0)
                {
                    return Invalid($"√ 미적용 {diff}장", tokens);
                }

                return Invalid("√ 초과 사용", tokens);
            }

            if (multiplyUsed != hand.MultiplyCount)
            {
                var diff = hand.MultiplyCount - multiplyUsed;
                if (diff > 0)
                {
                    return Invalid($"× 미사용 {diff}장", tokens);
                }

                return Invalid("× 초과 사용", tokens);
            }

            if (numbersUsed == 0)
            {
                return Invalid($"숫자 미사용 {hand.Numbers.Count}장", tokens);
            }

            if (binaryUsed != numbersUsed - 1)
            {
                return Invalid("연산자 수 불일치", tokens);
            }

            if (!ExpressionEvaluator.TryEvaluate(tokens, out var value, out var evaluationError))
            {
                return Invalid(evaluationError, tokens);
            }

            var expressionText = BuildExpressionString(tokens);
            return new ValidationResult
            {
                IsValid = true,
                Result = value,
                ExpressionText = expressionText,
                Tokens = tokens
            };
        }

        private static ValidationResult Invalid(string error, IReadOnlyList<ExpressionToken> tokens)
        {
            return new ValidationResult
            {
                IsValid = false,
                Error = error,
                Tokens = tokens ?? Array.Empty<ExpressionToken>()
            };
        }

        private static string BuildExpressionString(IReadOnlyList<ExpressionToken> tokens)
        {
            var builder = new StringBuilder();

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case ExpressionTokenType.UnaryOperator:
                        builder.Append('√');
                        break;
                    case ExpressionTokenType.Number:
                        if (builder.Length > 0 && builder[^1] != '√')
                        {
                            builder.Append(' ');
                        }

                        builder.Append(token.Number.ToString(CultureInfo.InvariantCulture));
                        break;
                    case ExpressionTokenType.BinaryOperator:
                        builder.Append(' ');
                        builder.Append(token.BinaryOperator.ToSymbol());
                        builder.Append(' ');
                        break;
                }
            }

            return builder.ToString().Trim();
        }
    }

    public static class ExpressionEvaluator
    {
        public static bool TryEvaluate(IReadOnlyList<ExpressionToken> tokens, out double result, out string error)
        {
            result = 0;
            error = string.Empty;

            if (tokens == null || tokens.Count == 0)
            {
                error = "숫자 미사용 0장";
                return false;
            }

            var values = new Stack<double>();
            var operators = new Stack<ExpressionToken>();

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case ExpressionTokenType.Number:
                        values.Push(token.Number);
                        ApplyPendingUnaryOperators(values, operators, ref error);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return false;
                        }

                        break;

                    case ExpressionTokenType.UnaryOperator:
                        operators.Push(token);
                        break;

                    case ExpressionTokenType.BinaryOperator:
                        while (operators.Count > 0 && operators.Peek().Type == ExpressionTokenType.BinaryOperator &&
                               GetPriority(operators.Peek().BinaryOperator) >= GetPriority(token.BinaryOperator))
                        {
                            if (!EvaluateBinary(values, operators.Pop().BinaryOperator, ref error))
                            {
                                return false;
                            }
                        }

                        operators.Push(token);
                        break;
                }
            }

            while (operators.Count > 0)
            {
                var op = operators.Pop();
                if (op.Type == ExpressionTokenType.UnaryOperator)
                {
                    if (!ApplyUnary(values, op, ref error))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!EvaluateBinary(values, op.BinaryOperator, ref error))
                    {
                        return false;
                    }
                }
            }

            if (values.Count != 1)
            {
                error = "연산 중 오류가 발생했습니다.";
                return false;
            }

            result = values.Pop();
            return true;
        }

        private static void ApplyPendingUnaryOperators(Stack<double> values, Stack<ExpressionToken> operators, ref string error)
        {
            while (operators.Count > 0 && operators.Peek().Type == ExpressionTokenType.UnaryOperator)
            {
                if (!ApplyUnary(values, operators.Pop(), ref error))
                {
                    return;
                }
            }
        }

        private static bool ApplyUnary(Stack<double> values, ExpressionToken token, ref string error)
        {
            if (values.Count == 0)
            {
                error = "토큰 시퀀스 오류";
                return false;
            }

            var value = values.Pop();
            switch (token.UnaryOperator)
            {
                case SpecialCardType.SquareRoot:
                    if (value < 0)
                    {
                        error = "√ 적용 불가";
                        return false;
                    }

                    values.Push(Math.Sqrt(value));
                    return true;
                default:
                    error = "지원하지 않는 단항 연산자";
                    return false;
            }
        }

        private static bool EvaluateBinary(Stack<double> values, OperatorType operatorType, ref string error)
        {
            if (values.Count < 2)
            {
                error = "연산 중 오류가 발생했습니다.";
                return false;
            }

            var right = values.Pop();
            var left = values.Pop();

            if (operatorType == OperatorType.Divide && Math.Abs(right) < double.Epsilon)
            {
                error = "0으로 나눌 수 없음";
                return false;
            }

            switch (operatorType)
            {
                case OperatorType.Add:
                    values.Push(left + right);
                    return true;
                case OperatorType.Subtract:
                    values.Push(left - right);
                    return true;
                case OperatorType.Multiply:
                    values.Push(left * right);
                    return true;
                case OperatorType.Divide:
                    values.Push(left / right);
                    return true;
                default:
                    error = "알 수 없는 연산자";
                    return false;
            }
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
