using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{
    [XmlRoot("WORLD")]
    public class World : NSApi
    {
        public World()
        {
            Happenings = new List<WorldEvent>();
        }

        [XmlArray("HAPPENINGS")]
        [XmlArrayItem("EVENT", typeof(WorldEvent))]
        public List<WorldEvent> Happenings;

        [XmlElement("REGIONS")]
        public string Regions;

        [XmlElement("FEATUREDREGION")]
        public string Featured;

        [XmlElement("NATIONS")]
        public string Nations;

        [XmlElement("NEWNATIONS")]
        public string NewNations;

        [XmlElement("NUMNATIONS")]
        public int NumNations;

        [XmlElement("NUMREGIONS")]
        public int NumRegions;
    }
}
