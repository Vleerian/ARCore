using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{
    [Serializable()]
    public sealed class NationCensus
    {
        [XmlAttribute("id")]
        public int CensusID;

        [XmlText]
        public int CensusScore;
    }

    /// <summary>
    /// A container for nation data-dump items
    /// We only save the things relevant to raiding
    /// </summary>
    /// 
    [Serializable()]
    public sealed class Nation : NSApi
    {
        public Nation()
        {
            CensusScores = new List<NationCensus>();
        }

        [XmlElement("NAME")]
        public string name;
        public string Name
        {
            get
            {
                return name.Replace(' ','_').ToLower();
            }
        }

        [XmlElement("UNSTATUS")]
        public string WAStatus;
        [XmlElement("ENDORSEMENTS")]
        public string Endorsements;
        [XmlElement("REGION")]
        public string Region;
        [XmlElement("TGCANRECRUIT")]
        public int CanRecruit;
        [XmlElement("TGCANCAMPAIGN")]
        public int CanCampaign;
        [XmlArray("CENSUS")]
        [XmlArrayItem("SCALE", typeof(NationCensus))]
        public List<NationCensus> CensusScores;

        //Elements added by ARCore
        public long Index;
        public long Votes;
    }
}
