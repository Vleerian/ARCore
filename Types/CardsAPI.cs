using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{   
    [XmlRoot("CARDS")]
    [Serializable()]
    public class CardsPlayerInfo : NSApi
    {
        [XmlElement("INFO")]
        public PlayerInfo PlayerInfo;
    }

    [XmlRoot("CARDS")]
    [Serializable()]
    public class CardsDeckInfo : NSApi
    {
        [XmlArray("DECK")]
        [XmlArrayItem("CARD", typeof(DeckEntry))]
        public List<DeckEntry> Deck;
    }

    [Serializable()]
    public class PlayerInfo
    {
        [XmlElement("BANK")]
        public float Bank;

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

    [Serializable()]
    public class DeckEntry
    {
        [XmlElement("CARDID")]
        public int CardID;

        [XmlElement("CATEGORY")]
        public string Rarity;

        [XmlElement("SEASON")]
        public int Season;
    }
}
