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
        const string RegionSQL = "INSERT INTO regions (name, nations, numnations, delegate, delegateauth, founder, factbook, lastupdate, firstnation, passworded, founderless) VALUES (@name, @nations, @numnations, @delegate, @delegateauth, @founder, @factbook, @lastupdate, @firstnation, @passworded, @founderless)";
        const string NationSQL = "INSERT INTO nations (name, region, endorsements, WAStatus) VALUES (@name, @region, @endorsements, @WAStatus)";

        private readonly IServiceProvider services;
        private readonly APIHandler API;

        public string User;

        public readonly UpdateDerivation MajorUpdate;
        public readonly UpdateDerivation MinorUpdate;

        private SQLiteConnection connection;

        // We keep these two cached because they are potentially common calls
        int NumNationsCache;
        int NumRegionsCache;

        public ARData(IServiceProvider services)
        {
            Logger.Log(LogEventType.Verbose, "Initializing ARData");

            this.services = services;
            API = services.GetRequiredService<APIHandler>();

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
        }

        /// <summary>
        /// Inserts a region into the WorldData database
        /// </summary>
        /// <param name="region">The region to be inserted</param>
        /// <param name="connection">The Database connection</param>
        /// <param name="transaction">The current transaction</param>
        /// <returns></returns>
        private async Task InsertRegion(Region region, bool passworded, bool founderless, SQLiteTransaction transaction)
        {
            var command = new SQLiteCommand(RegionSQL, connection, transaction);

            command.Parameters.AddWithValue("@name", region.Name);
            command.Parameters.AddWithValue("@nations", region.nations);
            command.Parameters.AddWithValue("@numnations", region.NumNations);
            command.Parameters.AddWithValue("@delegate", region.Delegate);
            command.Parameters.AddWithValue("@delegateauth", region.DelegateAuth);
            command.Parameters.AddWithValue("@founder", region.Founder);
            command.Parameters.AddWithValue("@factbook", region.Factbook);
            command.Parameters.AddWithValue("@lastupdate", (long)region.lastUpdate);
            command.Parameters.AddWithValue("@firstnation", region.Nations.Length > 0?region.Nations[0]:"");
            command.Parameters.AddWithValue("@passworded", passworded?1:0);
            command.Parameters.AddWithValue("@founderless", founderless?1:0);

            await command.ExecuteNonQueryAsync();
        }

        
        private async Task InsertNation(Nation nation,  SQLiteTransaction transaction)
        {
            var command = new SQLiteCommand(NationSQL, connection, transaction);

            command.Parameters.Add(new SQLiteParameter("@name", nation.Name));
            command.Parameters.Add(new SQLiteParameter("@region", nation.Region));
            command.Parameters.Add(new SQLiteParameter("@endorsements", nation.Endorsements.Split(":").Length));
            command.Parameters.Add(new SQLiteParameter("@WAstatus", nation.WAStatus));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Processes the supplied data dumps and inserts them into the conencted database
        /// </summary>
        /// <param name="nationDump"></param>
        /// <param name="regionDump"></param>
        /// <returns></returns>
        public async Task ProcessDumps(NationDataDump nationDump, RegionDataDump regionDump)
        {
            if(connection == null)
                throw new ApplicationException("No database connection established.");
            
            Logger.Log(LogEventType.Verbose, "Fetching password and founderless regions.");
            string[] passwordRegions;
            string[] founderlessRegions;
            {
                //Scoped to dispose of the NSAPIRequest objects once we're done with them
                API.Enqueue(out NSAPIRequest PasswordRequest, "https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=password");
                API.Enqueue(out NSAPIRequest FounderlessRequest, "https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=founderless");
                while(!PasswordRequest.Done && !FounderlessRequest.Done);

                var PasswordData = await PasswordRequest.GetResultAsync<World>();
                var FounderlessData = await PasswordRequest.GetResultAsync<World>();

                passwordRegions = PasswordData.Regions
                    .Replace('_',' ').ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries);
                founderlessRegions = FounderlessData.Regions
                    .Replace('_',' ').ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries);
            }

            Logger.Log(LogEventType.Information, "Building reigons Table.");
            using(var transaction = connection.BeginTransaction()){
                var tasks = regionDump.Regions.Select(region => {
                    // Determine if the region is passworded & founderless
                    bool passworded = passwordRegions.Any(R=>R==region.Name);
                    bool founderless = founderlessRegions.Any(R=>R==region.Name);
                    // Return the insert task
                    return InsertRegion( region, passworded, founderless, transaction );
                }
                );
                await Task.WhenAll(tasks);
                transaction.Commit();
            }

            Logger.Log(LogEventType.Information, "Building nations Table.");
            using(var transaction = connection.BeginTransaction()){
                var Tasks = nationDump.Nations.Select(Nation => InsertNation( Nation, transaction ));
                await Task.WhenAll(Tasks);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Initializes a new WorldData Database using the nationstates data dumps
        /// </summary>
        public async void InitializeDB()
        {
            var DBName = DateTime.Now.ToString("MM-dd-yy")+"_WorldData";
            // If there is a pre-existing database, return
            if(HelpersStatic.CongfigureDatabase(DBName, out connection))
                return;

            Logger.Log(LogEventType.Information, "Parsing region data dump, please wait...");
            RegionDataDump regionData = await API.DownloadDataDump<RegionDataDump>();

            Logger.Log(LogEventType.Information, "Parsing nation data dump, please wait...");
            NationDataDump nationData = await API.DownloadDataDump<NationDataDump>();
            
            await ProcessDumps(nationData, regionData);
        }

        /// <summary>
        /// Connects to a database
        /// </summary>
        /// <param name="DBName">The extensionless name of the database you want to connect to</param>
        /// <returns>True if the database already existed, false otherwise</returns>
        public bool ConnectToDB(string DBName)
        {
            bool Exists = File.Exists($"{DBName}.db");
            HelpersStatic.CongfigureDatabase(DBName, out connection);
            return Exists;
        }

        public Nation GetNation(string Nation)
        {
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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