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
        private readonly int numberCopiesPerValue;
        private readonly int multiplyCardsPerRound;
        private readonly int sqrtCardsPerRound;
        private readonly List<CardDefinition> slotDeck = new();
        private readonly Random random;

        public DeckService(int numberCopiesPerValue = 4, int multiplyCardsPerRound = 2, int sqrtCardsPerRound = 2, int? seed = null)
        {
            this.numberCopiesPerValue = Math.Max(1, numberCopiesPerValue);
            this.multiplyCardsPerRound = Math.Max(0, multiplyCardsPerRound);
            this.sqrtCardsPerRound = Math.Max(0, sqrtCardsPerRound);
            random = seed.HasValue ? new Random(seed.Value) : new Random();

            BuildSlotDeck();
        }

        /// <summary>
        /// 라운드 시작 시 슬롯 덱을 새로 구성합니다.
        /// </summary>
        public void BuildSlotDeck()
        {
            slotDeck.Clear();

            for (var number = 0; number <= 10; number++)
            {
                for (var copy = 0; copy < numberCopiesPerValue; copy++)
                {
                    slotDeck.Add(new CardDefinition(CardKind.Number, number, OperatorType.Add));
                }
            }

            for (var i = 0; i < multiplyCardsPerRound; i++)
            {
                slotDeck.Add(new CardDefinition(CardKind.Special, SpecialCardType.Multiply));
            }

            for (var i = 0; i < sqrtCardsPerRound; i++)
            {
                slotDeck.Add(new CardDefinition(CardKind.Special, SpecialCardType.SquareRoot));
            }

            Shuffle(slotDeck);
        }

        /// <summary>
        /// 분배 슬롯에서 한 장의 카드를 뽑습니다.
        /// 슬롯 덱이 소진될 경우 자동으로 재구성합니다.
        /// </summary>
        public CardDefinition DrawSlotCard()
        {
            if (slotDeck.Count == 0)
            {
                BuildSlotDeck();
            }

            var lastIndex = slotDeck.Count - 1;
            var card = slotDeck[lastIndex];
            slotDeck.RemoveAt(lastIndex);
            return card.Clone();
        }

        /// <summary>
        /// 숫자 카드만을 무한 샘플링하여 반환합니다. (0~10 범위)
        /// </summary>
        public CardDefinition DrawNumberCard()
        {
            var value = random.Next(0, 11);
            return new CardDefinition(CardKind.Number, value, OperatorType.Add);
        }

        private void Shuffle(List<CardDefinition> cards)
        {
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
            }
        }
    }
}
