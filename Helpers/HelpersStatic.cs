using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ARCore.Helpers
{
    // A container for helper methods used throughout ARCore
    public static class HelpersStatic
    {
        //Generic object deserializer
        public static Task<T> DeserializeObjectAsync<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using var fs = new StringReader(xml);
            T dump = (T)serializer.Deserialize(fs);
            return Task.FromResult(dump);
        }

        //Generic object deserializer
        public static T DeserializeObject<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using var fs = new StringReader(xml);
            T dump = (T)serializer.Deserialize(fs);
            return dump;
        }

        public static string SecondsToTime(double time) =>
            TimeSpan.FromSeconds(time).ToString(@"hh\:mm\:ss");

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}