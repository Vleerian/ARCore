using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{
    [Serializable()]
    public class WorldEvent
    {
        [XmlElement("TIMESTAMP")]
        public long Timestamp;
        [XmlElement("TEXT")]
        public string Text;
    }
}
