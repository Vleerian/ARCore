using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{
    [Serializable()]
    [XmlRoot("CARD")]
    public class CardMarket : NSApi
    {
        public CardMarket()
        {
            Markets = new List<Market>();
            Trades = new List<Trade>();
        }

        [XmlElement("CARDID")]
        public int CardID;

        [XmlElement("SEASON")]
        public int Season;

        [XmlElement("CATEGORY")]
        public string Rarity;

        [XmlElement("MARKET_VALUE")]
        public float MarketValue;

        [XmlArray("MARKETS")]
        [XmlArrayItem("MARKET", typeof(Market))]
        public List<Market> Markets;

        [XmlArray("TRADS")]
        [XmlArrayItem("TRADE", typeof(Market))]
        public List<Trade> Trades;
    }

    [Serializable()]
    public class Market
    {
        [XmlElement("NATION")]
        public string Nation;

        [XmlElement("PRICE")]
        public float Price;

        [XmlElement("TIMESTAMP")]
        public long Timestamp;

        [XmlElement("TYPE")]
        public string Type;
    }

    [Serializable()]
    public class Trade
    {
        [XmlElement("BUYER")]
        public string Buyer;

        [XmlElement("SELLER")]
        public string Seller;

        [XmlElement("PRICE")]
        public float? Price;

        [XmlElement("TIMESTAMP")]
        public long Timestamp;
    }

    [Serializable()]
    public class Auction
    {
        [XmlElement("NAME")]
        public string Name;

        [XmlElement("CATEGORY")]
        public string Rarity;

        [XmlElement("CARDID")]
        public string CardID;

        [XmlElement("SEASON")]
        public int Season;
    }
}