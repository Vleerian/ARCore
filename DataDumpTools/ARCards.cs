using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections.Generic;

using System.Data.SQLite;
using System.Data.Common;

using Microsoft.Extensions.DependencyInjection;

using ARCore.Core;
using ARCore.Types;
using ARCore.Helpers;

namespace ARCore.DataDumpTools
{   
    /// <summary>
    /// TODO: refactor the cards utilities to use a cards database
    /// CardCounter is the cards equivalent of ARData, and handles
    /// parsing, storing, and delivering cards data.
    /// </summary>
    [XmlRoot("CARDS")]
    public class ARCards
    {
        IServiceProvider Services;
        ARData Data;
        APIHandler API;
        
        private readonly SQLiteConnection connection;

        public ARCards(IServiceProvider services){
            Services = services;
            Data = Services.GetRequiredService<ARData>();
            API = Services.GetRequiredService<APIHandler>();

            // Once this uses a database, this terrible code will be gone
            // But in the meantime, this works.

            Logger.Log(LogEventType.Information, "Building CardData Database, this may take several minutes.");

            var DBName = "CardData.db";
            bool setup = !File.Exists(DBName);
            connection = new SQLiteConnection($"Data Source={DBName};Version=3;").OpenAndReturn();
            if(!setup)
            {
                Logger.Log(LogEventType.Information, "Connected to CardData Database");
                return;
            }

            Logger.Log(LogEventType.Information, "Setting up CardData Database");
            string DatabaseSetup = File.ReadAllText("./card_database.ddl");
            string[] Queries = DatabaseSetup.Split("|||");
            using(var transaction = connection.BeginTransaction())
            {
                foreach(string Query in Queries)
                new SQLiteCommand(Query, connection, transaction).ExecuteNonQuery();
                transaction.Commit();
            }

            // Load the card data dumps
            Logger.Log(LogEventType.Debug, "Parsing Cardset 1");
            var CardSet_1 = APIHandler.ParseDataDump<CardsDataDump>("cardlist_S1.xml.gz");

            Logger.Log(LogEventType.Debug, "Parsing Cardset 2");
            var CardSet_2 = APIHandler.ParseDataDump<CardsDataDump>("cardlist_S2.xml.gz");

            // Build the cards database
            Logger.Log(LogEventType.Information, "Building CardData. This may take some time.");
            using(var transaction = connection.BeginTransaction())
            {
                // Create tasks to insert the cards into the database
                var Set1Tasks = CardSet_1.CardSet.Cards
                    .Select(Card => SQLInsertCard(Card, CardSet_1.CardSet.Season, transaction));
                var Set2Tasks = CardSet_2.CardSet.Cards
                    .Select(Card => SQLInsertCard(Card, CardSet_2.CardSet.Season, transaction));
                
                // Combine the two sets of tasks
                Set1Tasks.ToList().AddRange(Set2Tasks);

                // Run and await all the tasks
                Task.WhenAll(Set1Tasks)
                    .GetAwaiter().GetResult();

                transaction.Commit();
            }
        }

        public async Task SQLInsertCard(Card card, int season, SQLiteTransaction transaction)
        {
            string SQL = "INSERT INTO `cards` (season, name, type, motto, category, region, cardcategory) VALUES (@season, @name, @type, @motto, @category, @region, @cardcategory);";
            var Command = new SQLiteCommand(SQL, connection);
            Command.Parameters.AddWithValue("@season", season);
            Command.Parameters.AddWithValue("@name", card.Name); 
            Command.Parameters.AddWithValue("@type", card.Type);
            Command.Parameters.AddWithValue("@motto", card.Motto);
            Command.Parameters.AddWithValue("@category", card.Category);
            Command.Parameters.AddWithValue("@region", card.Region);
            Command.Parameters.AddWithValue("@cardcategory", card.Rarity);
            await Command.ExecuteNonQueryAsync();
        }

        const string CardsEndpoint = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+deck+info;nationname=";
        public async Task<CardsAPI> GetPlayerInfo(string NationName)
        {
            API.Enqueue(out NSAPIRequest Request, CardsEndpoint+NationName);
            return await Request.GetResultAsync<CardsAPI>();
        }

        public Card GetCard(string CardName, int Season) =>
            GetCardAsync(CardName, Season).GetAwaiter().GetResult();

        public async Task<Card> GetCardAsync(string CardName, int Season)
        {
            // Format the input, and make a query for the card
            CardName = CardName.ToLower().Replace(' ', '_');
            string SQL = "SELECT * FROM `cards` WHERE name=@name AND season=@season;";
            var command = new SQLiteCommand(SQL, connection);
            command.Parameters.AddWithValue("@name", CardName);
            command.Parameters.AddWithValue("@season", Season);

            using(var dbReader = await command.ExecuteReaderAsync())
            {
                while(dbReader.Read())
                {
                    try{
                        return new Card()
                        {
                            Season = (int)dbReader.GetInt64(1),
                            name = dbReader.GetString(2),
                            Type = dbReader.GetString(2),
                            Motto = dbReader.GetString(3),
                            Category = dbReader.GetString(4),
                            Region = dbReader.GetString(5),
                            Rarity = dbReader.GetString(6)
                        };
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogEventType.Error, $"Error fetching {CardName} (S{Season})", e);
                    }
                    
                }
            }
            return null;
        }

        public Card[] GetCard(string CardName) =>
            GetCardAsync(CardName).GetAwaiter().GetResult();

        public async Task<Card[]> GetCardAsync(string CardName)
        {
            // Format the input, and make a query for the card
            CardName = CardName.ToLower().Replace(' ', '_');
            string SQL = "SELECT * FROM `cards` WHERE name=@name";
            var command = new SQLiteCommand(SQL, connection);
            command.Parameters.AddWithValue("@name", CardName);

            // In order to be forward-compatible, we return a list (casted later to an array)
            // This ensures that additional seasons of cards don't require maintenance on ARCore's part
            List<Card> cards = new List<Card>();
            using(var dbReader = await command.ExecuteReaderAsync())
            {
                while(dbReader.Read())
                {
                    try{
                        cards.Add(new Card()
                        {
                            Season = (int)dbReader.GetInt64(1),
                            name = dbReader.GetString(2),
                            Type = dbReader.GetString(2),
                            Motto = dbReader.GetString(3),
                            Category = dbReader.GetString(4),
                            Region = dbReader.GetString(5),
                            Rarity = dbReader.GetString(6)
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogEventType.Error, $"Error fetching {CardName}", e);
                    }
                    
                }
            }
            return cards.ToArray();
        }
    }
}
