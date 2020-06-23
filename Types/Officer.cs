using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ARCore.Types
{
    [Serializable()]
    public sealed class Officer
    {
        [XmlElement("NATION")]
        public readonly string Nation;
        [XmlElement("OFFICE")]
        public readonly string Office;
        [XmlElement("AUTHORITY")]
        public readonly string OfficerAuth;
        [XmlElement("TIME")]
        public readonly int AssingedTimestamp;
        [XmlElement("BY")]
        public readonly string AssignedBy;
        [XmlElement("ORDER")]
        public readonly int Order;
    }
}
