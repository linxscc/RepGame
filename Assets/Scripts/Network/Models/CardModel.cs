
using System;

namespace RepGamebackModels
{
    // Define the CardModel class
    public class CardModel
    {
        public string CardID { get; set; }
        public CardType Type { get; set; }

        public CardType GetCardType(string cardName)
        {
            if (Enum.TryParse(cardName, out CardType cardType))
            {
                return cardType;
            }
            return CardType.木匠学徒; // Default to a valid type if parsing fails
        }
    }

    // Define the CardType enum based on the provided card names
    public enum CardType
    {
        木匠学徒 = 0,
        木匠 = 1,
        百工专家 = 2,
        铁匠学徒 = 3,
        铁匠 = 4,
        医学生 = 5,
        医师 = 6,
        农民学徒,
        农民,
        艺术学徒,
        梦想家,
        艺术家,
        设计学徒,
        设计师,
        兵卒,
        老兵,
        统领,
        道童,
        道士,
        住持,
        沙弥,
        和尚,
    }

}  