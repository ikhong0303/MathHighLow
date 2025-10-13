using System;
using MathHighLow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MathHighLow.UI
{
    /// <summary>
    /// 카드 버튼의 클릭 이벤트와 라벨링을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardButtonView : MonoBehaviour
    {
        [SerializeField] private Text label;

        private Button button;
        private CardDefinition card;
        private Action<CardButtonView> onClicked;

        public CardDefinition Card => card;

        public bool Interactable
        {
            get => button != null && button.interactable;
            set
            {
                if (button != null)
                {
                    button.interactable = value;
                }
            }
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            if (label == null)
            {
                label = GetComponentInChildren<Text>();
            }
        }

        public void Initialize(CardDefinition definition, Action<CardButtonView> clicked)
        {
            card = definition;
            onClicked = clicked;
            if (label != null)
            {
                label.text = card.GetDisplayText();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            onClicked?.Invoke(this);
        }
    }
}
