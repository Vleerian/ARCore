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

using System.Data.SQLite;
using System.Data.Common;

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

        /// <summary>
        /// Sets up a SQLLite database, and returns a connection to it
        /// Requires {DatabaseName.ddl} to exist for setup
        /// </summary>
        /// <param name="DatabaseName">The extensionless name of the database to connect to</param>
        /// <returns>True if the database already existed</returns>
        public static bool CongfigureDatabase(string DatabaseName, out SQLiteConnection connection)
        {
            Logger.Log(LogEventType.Information, $"Connecting to {DatabaseName}");
            bool setup = !File.Exists(DatabaseName);
            connection = new SQLiteConnection($"Data Source={DatabaseName}.db;Version=3;").OpenAndReturn();
            if(!setup)
            {
                Logger.Log(LogEventType.Information, $"Connected to {DatabaseName}");
                return true;
            }

            Logger.Log(LogEventType.Information, $"Setting up {DatabaseName}");
            string DatabaseSetup = File.ReadAllText("./{DatabaseName}.ddl");
            string[] Queries = DatabaseSetup.Split("|||");
            using(var transaction = connection.BeginTransaction())
            {
                foreach(string Query in Queries)
                new SQLiteCommand(Query, connection, transaction).ExecuteNonQuery();
                transaction.Commit();
            }

            return false;
        }
    }
}