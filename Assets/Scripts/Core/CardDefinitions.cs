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
        Operator
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
    /// 한 장의 카드 정보를 나타냅니다.
    /// </summary>
    [Serializable]
    public class CardDefinition
    {
        [SerializeField] private CardKind kind;
        [SerializeField] private int numberValue;
        [SerializeField] private OperatorType operatorValue;

        public CardDefinition(CardKind kind, int numberValue, OperatorType operatorValue)
        {
            this.kind = kind;
            this.numberValue = numberValue;
            this.operatorValue = operatorValue;
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

        public CardDefinition Clone()
        {
            return new CardDefinition(kind, numberValue, operatorValue);
        }

        public string GetDisplayText()
        {
            return kind switch
            {
                CardKind.Number => numberValue.ToString(),
                CardKind.Operator => operatorValue.ToSymbol(),
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
