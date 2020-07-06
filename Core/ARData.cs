using System.Linq;
using System.IO;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Data.SQLite;
using System.Data.Common;

using Newtonsoft;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;

using ARCore.Types;
using ARCore.Helpers;
using ARCore.RealTimeTools;

namespace ARCore.Core
{
    public class ARData
    {
        private readonly IServiceProvider _services;
        private readonly APIHandler _APIHandler;

        public string User;

        public readonly UpdateDerivation MajorUpdate;
        public readonly UpdateDerivation MinorUpdate;

        private readonly SQLiteConnection connection;

        // We keep these two cached because they are potentially common calls
        int NumNationsCache;
        int NumRegionsCache;

        public ARData(IServiceProvider services)
        {
            Logger.Log(LogEventType.Verbose, "Initializing ARData");

            _services = services;
            _APIHandler = services.GetRequiredService<APIHandler>();

            NumNationsCache = 0;
            NumRegionsCache = 0;

            Logger.Log(LogEventType.Debug, "Accessing update data from Atagait.com");
            Dictionary<string, UpdateDerivation> updateData;
            using (var client = new WebClient())
            {
                var json = client.DownloadString("https://atagait.com/python-bin/updateData.json");
                updateData = JsonConvert.DeserializeObject<Dictionary<string,UpdateDerivation>>(json);
            }
            MajorUpdate = updateData["major"];
            MinorUpdate = updateData["minor"];

            // TODO: Create a helper function to set up database
            // It should take `string database_structure_file, `string database_name`
            // And return a SQLiteConnection
            Logger.Log(LogEventType.Information, "Building WorldData Database, this may take several minutes.");

            var DBName = DateTime.Now.ToString("MM-dd-yy")+"_WorldData.db";
            bool setup = !File.Exists(DBName);
            connection = new SQLiteConnection($"Data Source={DBName};Version=3;").OpenAndReturn();
            if(!setup)
            {
                Logger.Log(LogEventType.Information, "Connected to WorldData Database");
                return;
            }

            Logger.Log(LogEventType.Information, "Setting up WorldData Database");
            string DatabaseSetup = File.ReadAllText("./database.ddl");
            string[] Queries = DatabaseSetup.Split("|||");
            using(var transaction = connection.BeginTransaction())
            {
                foreach(string Query in Queries)
                new SQLiteCommand(Query, connection, transaction).ExecuteNonQuery();
                transaction.Commit();
            }

            Logger.Log(LogEventType.Verbose, "Performing startup API requests.");
            string[] passwordRegions;
            string[] founderlessRegions;
            using (var client = new WebClient()){
                Logger.Log(LogEventType.Debug, "Requesting password regions.");
                client.Headers.Add("user-agent", $"ARCore - doomjaw@hotmail.com | Current User : {User}");
                var passwordData = client.DownloadString("https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=password");
                passwordRegions = HelpersStatic.DeserializeObject<World>(passwordData).Regions
                    .Replace('_',' ').ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries);

                Logger.Log(LogEventType.Debug, "Requesting founderless regions.");
                client.Headers.Add("user-agent", $"ARCore - doomjaw@hotmail.com | Current User : {User}");
                var founderData = client.DownloadString("https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=founderless");
                founderlessRegions = HelpersStatic.DeserializeObject<World>(founderData).Regions
                    .Replace('_',' ').ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries);
            }

            Logger.Log(LogEventType.Information, "Parsing region data dump, please wait...");
            RegionDataDump regionData;
            string RDataFilename = DateTime.Now.ToString("MM-dd-yy")+"regions.xml.gz";
            if(!File.Exists(RDataFilename))
                regionData = _APIHandler.DownloadDataDump<RegionDataDump>().GetAwaiter().GetResult();
            else
                regionData = APIHandler.ParseDataDump<RegionDataDump>(RDataFilename);
            
            Logger.Log(LogEventType.Information, "Building reigons Table.");
            using(var transaction = connection.BeginTransaction()){
                foreach(Region region in regionData.Regions)
                {
                    var command = new SQLiteCommand("INSERT INTO regions (name, nations, numnations, delegate, delegateauth, founder, factbook, lastupdate, firstnation, passworded, founderless) VALUES (@name, @nations, @numnations, @delegate, @delegateauth, @founder, @factbook, @lastupdate, @firstnation, @passworded, @founderless)");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    command.Parameters.AddWithValue("@name", region.Name);
                    command.Parameters.AddWithValue("@nations", region.nations);
                    command.Parameters.AddWithValue("@numnations", region.NumNations);
                    command.Parameters.AddWithValue("@delegate", region.Delegate);
                    command.Parameters.AddWithValue("@delegateauth", region.DelegateAuth);
                    command.Parameters.AddWithValue("@founder", region.Founder);
                    command.Parameters.AddWithValue("@factbook", region.Factbook);
                    command.Parameters.AddWithValue("@lastupdate", (long)region.lastUpdate);
                    command.Parameters.AddWithValue("@firstnation", region.Nations.Length > 0?region.Nations[0]:"");
                    command.Parameters.AddWithValue("@passworded", passwordRegions.Any(R=>R==region.Name)?1:0);
                    command.Parameters.AddWithValue("@founderless", founderlessRegions.Any(R=>R==region.Name)?1:0);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }

            Logger.Log(LogEventType.Information, "Parsing nation data dump, please wait...");
            NationDataDump nationData;
            string NDataFilename = DateTime.Now.ToString("MM-dd-yy")+"nations.xml.gz";
            if(!File.Exists(NDataFilename))
                nationData = _APIHandler.DownloadDataDump<NationDataDump>().GetAwaiter().GetResult();
            else
                nationData = APIHandler.ParseDataDump<NationDataDump>(NDataFilename);
            
            Logger.Log(LogEventType.Information, "Building nations Table.");
            using(var transaction = connection.BeginTransaction()){
                foreach(Nation nation in nationData.Nations)
                {
                    var command = new SQLiteCommand("INSERT INTO nations (name, region, endorsements, WAStatus) VALUES (@name, @region, @endorsements, @WAStatus)");
                    command.Connection = connection;
                    command.Transaction = transaction;
                    
                    command.Parameters.Add(new SQLiteParameter("@name", nation.Name));
                    command.Parameters.Add(new SQLiteParameter("@region", nation.Region));
                    command.Parameters.Add(new SQLiteParameter("@endorsements", nation.Endorsements.Split(":").Length));
                    command.Parameters.Add(new SQLiteParameter("@WAstatus", nation.WAStatus));
                    
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public Nation GetNation(string Nation)
        {
            var command = new SQLiteCommand("SELECT * FROM nations WHERE name=@name", connection);
            command.Parameters.Add(new SQLiteParameter("@name", Nation));

            using(var dbReader = command.ExecuteReader())
            {
                while(dbReader.Read())
                {
                    return new Nation(){
                        Index = dbReader.GetInt64(0),
                        name = dbReader.GetString(1),
                        Region = dbReader.GetString(2),
                        Votes = dbReader.GetInt64(3),
                        WAStatus = dbReader.GetString(4)
                    };
                }
            }

            return null;
        }

        public async Task<Nation> GetNationAsync(string Nation)
        {
            var command = new SQLiteCommand("SELECT * FROM nations WHERE name=@name", connection);
            command.Parameters.Add(new SQLiteParameter("@name", Nation));

            using(var dbReader = await command.ExecuteReaderAsync())
            {
                while(dbReader.Read())
                {
                    return new Nation(){
                        Index = dbReader.GetInt64(0),
                        name = dbReader.GetString(1),
                        Region = dbReader.GetString(2),
                        Votes = dbReader.GetInt64(3),
                        WAStatus = dbReader.GetString(4)
                    };
                }
            }

            return null;
        }
            

        public Region GetRegion(string Region)
        {
            var command = new SQLiteCommand("SELECT * FROM regions WHERE name=@name", connection);
            command.Parameters.Add(new SQLiteParameter("@name", Region));

            using(var dbReader = command.ExecuteReader())
            {
                while(dbReader.Read())
                {
                    try{
                        return new Region()
                        {
                            Index = dbReader.GetInt64(0),
                            name = dbReader.GetString(1),
                            nations = dbReader.GetString(2),
                            NumNations = (int)dbReader.GetInt64(3),
                            Delegate = dbReader.GetString(4),
                            DelegateAuth = dbReader.GetString(5),
                            Founder = dbReader.GetString(6),
                            Factbook = dbReader.GetString(7),
                            lastUpdate = (double)dbReader.GetInt64(8),
                            FirstNation = dbReader.GetString(9),
                            hasPassword = dbReader.GetInt32(10)==1,
                            hasFounder = dbReader.GetInt32(11)==1,
                        };
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogEventType.Error, $"Error fetching {Region}", e);
                    }
                    
                }
            }

            return null;
        }

        public async Task<Region> GetRegionAsync(string Region)
        {
            var command = new SQLiteCommand("SELECT * FROM regions WHERE name=@name", connection);
            command.Parameters.Add(new SQLiteParameter("@name", Region));

            using(var dbReader = await command.ExecuteReaderAsync())
            {
                while(dbReader.Read())
                {
                    return new Region()
                    {
                        Index = dbReader.GetInt64(0),
                        name = dbReader.GetString(1),
                        nations = dbReader.GetString(2),
                        NumNations = (int)dbReader.GetInt64(3),
                        Delegate = dbReader.GetString(4),
                        DelegateAuth = dbReader.GetString(5),
                        Founder = dbReader.GetString(6),
                        Factbook = dbReader.GetString(7),
                        lastUpdate = (double)dbReader.GetInt64(8),
                        FirstNation = dbReader.GetString(9),
                        hasPassword = dbReader.GetInt32(10)==1,
                        hasFounder = dbReader.GetInt32(11)==1,
                    };
                }
            }

            return null;
        }

        // Return the number of nations
        public int NumNations()
        {
            if(NumNationsCache == 0)
            {
                var command = new SQLiteCommand("SELECT COUNT(*) FROM nations", connection);
                using(var dbReader = command.ExecuteReader())
                {
                    while(dbReader.Read())
                    {
                        NumNationsCache = (int)dbReader.GetInt64(0);
                    }
                }
            }
            return NumNationsCache;
        }

        // Return the number of regions
        public int NumRegions()
        {
            if(NumRegionsCache == 0)
            {
                var command = new SQLiteCommand("SELECT COUNT(*) FROM regions", connection);
                using(var dbReader = command.ExecuteReader())
                {
                    while(dbReader.Read())
                    {
                        NumRegionsCache = (int)dbReader.GetInt64(0);
                    }
                }
            }
            return NumRegionsCache;
        }

        public List<string> GetRegions()
        {
            var command = new SQLiteCommand("SELECT name FROM regions", connection);
            List<string> Regions = new List<string>();
            using(var dbReader = command.ExecuteReader())
            {
                while(dbReader.Read())
                {
                    Regions.Add(dbReader.GetString(0));
                }
            }
            return Regions;
        }

        public double TimePerNation(bool Major)
        {
            // Some castings are required to ensure that we get a double
            double nationCount = NumNations();
            double UpLength = (double)(Major?MajorUpdate.UpdateLength:MinorUpdate.UpdateLength);
            
            return UpLength / nationCount;
        }

        public double UpdateLength(bool Major)
        {
            if(Major)
                return MajorUpdate.UpdateLength;
            return MinorUpdate.UpdateLength;
        }
    }
}