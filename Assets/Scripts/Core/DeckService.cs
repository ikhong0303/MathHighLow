using System;
using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Core
{
    /// <summary>
    /// 숫자 및 연산자 카드 덱을 생성하고 관리합니다.
    /// </summary>
    public class DeckService
    {
        private readonly int minNumber;
        private readonly int maxNumber;
        private readonly int numberCopies;
        private readonly int operatorCopies;
        private readonly List<CardDefinition> cards = new();
        private readonly Random random;

        public DeckService(int minNumber = 0, int maxNumber = 9, int numberCopies = 2, int operatorCopies = 2, int? seed = null)
        {
            if (minNumber > maxNumber)
            {
                throw new ArgumentException("최소 숫자는 최대 숫자보다 클 수 없습니다.");
            }

            this.minNumber = minNumber;
            this.maxNumber = maxNumber;
            this.numberCopies = Math.Max(1, numberCopies);
            this.operatorCopies = Math.Max(1, operatorCopies);
            random = seed.HasValue ? new Random(seed.Value) : new Random();

            BuildDeck();
        }

        public int Count => cards.Count;

        /// <summary>
        /// 덱을 초기 상태로 재구성합니다.
        /// </summary>
        public void BuildDeck()
        {
            cards.Clear();

            for (var i = minNumber; i <= maxNumber; i++)
            {
                for (var copy = 0; copy < numberCopies; copy++)
                {
                    cards.Add(new CardDefinition(CardKind.Number, i, OperatorType.Add));
                }
            }

            foreach (OperatorType operatorType in Enum.GetValues(typeof(OperatorType)))
            {
                for (var copy = 0; copy < operatorCopies; copy++)
                {
                    cards.Add(new CardDefinition(CardKind.Operator, 0, operatorType));
                }
            }

            Shuffle();
        }

        public void Shuffle()
        {
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
            }
        }

        public CardDefinition Draw()
        {
            if (cards.Count == 0)
            {
                BuildDeck();
            }

            var lastIndex = cards.Count - 1;
            var card = cards[lastIndex];
            cards.RemoveAt(lastIndex);
            return card;
        }

        public List<CardDefinition> Draw(int count)
        {
            if (count <= 0)
            {
                return new List<CardDefinition>();
            }

            var results = new List<CardDefinition>(count);
            for (var i = 0; i < count; i++)
            {
                results.Add(Draw());
            }

            return results;
        }

        public IEnumerable<CardDefinition> PeekAll()
        {
            return cards.Select(card => card.Clone());
        }
    }
}
