using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{   
    [XmlRoot("CARDS")]
    public class CardsAPI : NSApi
    {
        [XmlElement("DECK", typeof(Card))]
        public List<Card> Deck;

        [XmlElement("INFO", typeof(CardPlayerInfo))]
        public CardPlayerInfo PlayerInfo;
    }

    [Serializable()]
    public class CardPlayerInfo
    {
        [XmlElement("BANK")]
        public int Bank;

        [XmlElement("DECK_CAPACITY_RAW")]
        public int Deck_Capacity;

        [XmlElement("DECK_VALUE")]
        public float Deck_Value;

        [XmlElement("ID")]
        public int ID;

        [XmlElement("NAME")]
        public string PlayerName;

        [XmlElement("NUM_CARDS")]
        public string CardCount;
    }
}
