using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{   
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

        // ARCore added elements
        public int Season;
    }
}
