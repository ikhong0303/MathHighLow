using System;
using UnityEngine;

namespace MathHighLow.Core
{
    /// <summary>
    /// 카드 유형을 구분합니다.
    /// </summary>
    public enum CardKind
    {
        Number,
        Operator,
        Special
    }

    /// <summary>
    /// 사칙연산 기호를 정의합니다.
    /// </summary>
    public enum OperatorType
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>
    /// 특수 카드 유형을 정의합니다.
    /// </summary>
    public enum SpecialCardType
    {
        Multiply,
        SquareRoot
    }

    /// <summary>
    /// 한 장의 카드 정보를 나타냅니다.
    /// </summary>
    [Serializable]
    public class CardDefinition
    {
        [SerializeField] private CardKind kind;
        [SerializeField] private int numberValue;
        [SerializeField] private OperatorType operatorValue;
        [SerializeField] private SpecialCardType specialType;

        public CardDefinition(CardKind kind, int numberValue, OperatorType operatorValue)
        {
            this.kind = kind;
            this.numberValue = numberValue;
            this.operatorValue = operatorValue;
        }

        public CardDefinition(CardKind kind, SpecialCardType specialType)
        {
            this.kind = kind;
            this.specialType = specialType;
        }

        public CardKind Kind => kind;

        public int NumberValue
        {
            get => numberValue;
            set => numberValue = value;
        }

        public OperatorType OperatorValue
        {
            get => operatorValue;
            set => operatorValue = value;
        }

        public SpecialCardType SpecialType
        {
            get => specialType;
            set => specialType = value;
        }

        public bool IsSpecial => kind == CardKind.Special;

        public bool IsUnary => IsSpecial && specialType == SpecialCardType.SquareRoot;

        public CardDefinition Clone()
        {
            var clone = new CardDefinition(kind, numberValue, operatorValue)
            {
                SpecialType = specialType
            };
            return clone;
        }

        public string GetDisplayText()
        {
            return kind switch
            {
                CardKind.Number => numberValue.ToString(),
                CardKind.Operator => operatorValue.ToSymbol(),
                CardKind.Special => specialType switch
                {
                    SpecialCardType.Multiply => "×",
                    SpecialCardType.SquareRoot => "√",
                    _ => "?"
                },
                _ => "?"
            };
        }
    }

    public static class OperatorExtensions
    {
        public static string ToSymbol(this OperatorType type)
        {
            return type switch
            {
                OperatorType.Add => "+",
                OperatorType.Subtract => "-",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "?"
            };
        }

        public static double Apply(this OperatorType type, double left, double right)
        {
            return type switch
            {
                OperatorType.Add => left + right,
                OperatorType.Subtract => left - right,
                OperatorType.Multiply => left * right,
                OperatorType.Divide => right == 0 ? double.PositiveInfinity : left / right,
                _ => double.NaN
            };
        }
    }
}
