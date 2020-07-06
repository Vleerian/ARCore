using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{   
    public class CardPair
    {
        public Card Season1;
        public Card Season2;
    }

    [Serializable()]
    public class Trophy
    {
        [XmlAttribute("type")]
        public string TrophyType;

        [XmlText]
        public string Score;
    }

    [Serializable()]
    public class Card : NSApi
    {
        public Card(){
            Trophies = new List<Trophy>();
        }

        [XmlElement("ID")]
        public int ID;

        [XmlElement("NAME")]
        public string name;
        public string Name {
            get { return name.ToLower().Replace(" ", "_"); }
        }

        [XmlElement("TYPE")]
        public string Type;

        [XmlElement("MOTTO")]
        public string Motto;

        [XmlElement("CATEGORY")]
        public string Category;

        [XmlArray("BADGE")]
        [XmlArrayItem("BADGE", typeof(string))]
        public string[] Badges;

        [XmlArray("TROPHIES")]
        [XmlArrayItem("TROPHY", typeof(Trophy))]
        public List<Trophy> Trophies;

        [XmlElement("REGION")]
        public string Region;

        [XmlElement("CARDCATEGORY")]
        public string Rarity;
    }

    /*
    [Serializable()]
    public class Market : Card
    {
        [XmlElement("NATION")]
        [XmlElement("NAME")]
        public string Nation;

        [XmlElement("PRICE")]
        public float Price;

        [XmlElement("TIMESTAMP")]
        public long Timestamp;

        [XmlElement("TYPE")]
        public string MarketType;

        
        [XmlElement("MARKET_VALUE")]
        public float MarketValue;

        [XmlElement("TRADES", typeof(Trade))]
        public List<Trade> Trades;

        [XmlElement("AUCTIONS", typeof(Market))]
        public List <Market> Auctions;
        
        [XmlElement("MARKETS")]
        public List <Market> Markets;
    }

    [Serializable()]
    public class Trade : Market
    {
        [XmlElement("BUYER")]
        public string Buyer;

        [XmlElement("SELLER")]
        public string Seller;
    }
    */

}
