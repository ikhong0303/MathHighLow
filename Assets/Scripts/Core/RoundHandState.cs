using System;
using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Core
{
    /// <summary>
    /// 한 라운드에서 공유되는 손패 상태를 표현합니다.
    /// </summary>
    public class RoundHandState
    {
        private static readonly OperatorType[] BaseOperators =
        {
            OperatorType.Add,
            OperatorType.Subtract,
            OperatorType.Divide
        };

        private readonly List<int> numbers = new();
        private readonly HashSet<OperatorType> disabledBaseOperators = new();

        public IReadOnlyList<int> Numbers => numbers;

        public int MultiplyCount { get; private set; }

        public int SquareRootCount { get; private set; }

        public IReadOnlyCollection<OperatorType> DisabledBaseOperators => disabledBaseOperators;

        public void Reset()
        {
            numbers.Clear();
            disabledBaseOperators.Clear();
            MultiplyCount = 0;
            SquareRootCount = 0;
        }

        public void AddNumberCard(int value)
        {
            numbers.Add(value);
        }

        public void AddSpecialCard(SpecialCardType specialType)
        {
            switch (specialType)
            {
                case SpecialCardType.Multiply:
                    MultiplyCount++;
                    break;
                case SpecialCardType.SquareRoot:
                    SquareRootCount++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specialType), specialType, null);
            }
        }

        public bool DisableBaseOperator(OperatorType operatorType)
        {
            if (!IsBaseOperator(operatorType))
            {
                return false;
            }

            return disabledBaseOperators.Add(operatorType);
        }

        public bool IsOperatorEnabled(OperatorType operatorType)
        {
            if (operatorType == OperatorType.Multiply)
            {
                return MultiplyCount > 0;
            }

            if (!IsBaseOperator(operatorType))
            {
                return false;
            }

            return !disabledBaseOperators.Contains(operatorType);
        }

        public IReadOnlyList<OperatorType> GetAvailableBaseOperators()
        {
            return BaseOperators.Where(operatorType => !disabledBaseOperators.Contains(operatorType)).ToList();
        }

        public RoundHandSnapshot CreateSnapshot()
        {
            return new RoundHandSnapshot(numbers, MultiplyCount, SquareRootCount, disabledBaseOperators);
        }

        private static bool IsBaseOperator(OperatorType operatorType)
        {
            return operatorType is OperatorType.Add or OperatorType.Subtract or OperatorType.Divide;
        }
    }

    /// <summary>
    /// 라운드 손패 정보를 불변 스냅샷 형태로 제공합니다.
    /// </summary>
    public readonly struct RoundHandSnapshot
    {
        public RoundHandSnapshot(IEnumerable<int> numbers, int multiplyCount, int sqrtCount, IEnumerable<OperatorType> disabled)
        {
            Numbers = numbers?.ToArray() ?? Array.Empty<int>();
            MultiplyCount = Math.Max(0, multiplyCount);
            SquareRootCount = Math.Max(0, sqrtCount);
            DisabledBaseOperators = new HashSet<OperatorType>(disabled ?? Array.Empty<OperatorType>());
        }

        public IReadOnlyList<int> Numbers { get; }

        public int MultiplyCount { get; }

        public int SquareRootCount { get; }

        public IReadOnlyCollection<OperatorType> DisabledBaseOperators { get; }

        public IReadOnlyList<OperatorType> GetEnabledBaseOperators()
        {
            var disabled = DisabledBaseOperators;

            return new List<OperatorType>
            {
                OperatorType.Add,
                OperatorType.Subtract,
                OperatorType.Divide
            }.Where(op => !disabled.Contains(op)).ToList();
        }
    }
}
