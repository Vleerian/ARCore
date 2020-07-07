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
        // TODO: Find a way to dynamically determine how many seasons of cards there are.
        public const int Card_Seasons = 2;
        IServiceProvider Services;
        ARData Data;
        APIHandler API;
        
        const string PlayerInfoEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+info;nationname=";
        const string PlayerDeckEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+deck;nationname=";
        const string CardMarketEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=card+markets;";
        const string CardTradesEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=card+trades;";
        const string AllAuctionsEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+auctions";
        const string AllTradesEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+trades";

        private SQLiteConnection connection;

        public ARCards(IServiceProvider services){
            Services = services;
            Data = Services.GetRequiredService<ARData>();
            API = Services.GetRequiredService<APIHandler>();
        }

        /// <summary>
        /// Initializes the cards database, and sets it up if it has not been configured piror
        /// </summary>
        public async Task InitializeDB()
        {
            Logger.Log(LogEventType.Information, "Building CardData Database, this may take several minutes.");

            if(HelpersStatic.CongfigureDatabase("CardData", out connection))
                return;

            // Load the card data dumps
            for(int i = 1; i < Card_Seasons+1; i++)
            {
                Logger.Log(LogEventType.Debug, $"Parsing Cardset {i}");
                var CardSet = APIHandler.ParseDataDump<CardsDataDump>($"cardlist_S{i}.xml.gz");

                Logger.Log(LogEventType.Information, $"Building CardData for set {i}. This may take some time.");
                using(var transaction = connection.BeginTransaction())
                {
                    // Create tasks to insert the cards into the database
                    var Tasks = CardSet.CardSet.Cards
                        .Select(Card => SQLInsertCard(Card, CardSet.CardSet.Season, transaction));

                    // Run and await all the tasks
                    await Task.WhenAll(Tasks);

                    transaction.Commit();
                }
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

        /// <summary>
        /// Shorthand method to retrieve player cards information
        /// </summary>
        /// <param name="NationName">The nation you want cards info for</param>
        public async Task<CardsAPI> GetPlayerInfoAsync(string NationName)
        {
            API.Enqueue(out NSAPIRequest Request, PlayerInfoEnd+NationName);
            return await Request.GetResultAsync<CardsAPI>();
        }

        /// <summary>
        /// Shorthand method to retrieve player deck information
        /// </summary>
        /// <param name="NationName">The nation you want deck info for</param>
        public async Task<CardsAPI> GetPlayerDeckInfoASync(string NationName)
        {
            API.Enqueue(out NSAPIRequest Request, PlayerDeckEnd+NationName);
            return await Request.GetResultAsync<CardsAPI>();
        }

        /// <summary>
        /// Shorthand method to retrieve market info for a card
        /// </summary>
        /// <param name="CardName">The name of the card you want info about</param>
        /// <param name="Season">The season that card is in</param>
        /// <returns></returns>
        public async Task<CardMarket> GetCardMarketAsync(string CardName, int Season)
        {
            var Card = await GetCardAsync(CardName, Season);
            if(Card == null)
                return null;
            API.Enqueue(out NSAPIRequest Request, CardMarketEnd+$"cardid={Card.ID};season={Season}");
            return await Request.GetResultAsync<CardMarket>();
        }

        /// <summary>
        /// Retrieves card information from the database, limited to one season
        /// </summary>
        /// <param name="CardName">The card you want information about</param>
        /// <param name="Season">What season to look for</param>
        /// <returns>The specific card retrieved, or null if none found</returns>
        public async Task<Card> GetCardAsync(string CardName, int Season)
        {
            if(connection == null)
                throw new ApplicationException("No database connection established.");

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

        /// <summary>
        /// Retrieves card information from the database
        /// </summary>
        /// <param name="CardName">The card you want information about</param>
        /// <param name="Season">What season to look for</param>
        /// <returns>An array of retrieved cards, an empty array if none are found</returns>
        public async Task<Card[]> GetCardAsync(string CardName)
        {
            if(connection == null)
                throw new ApplicationException("No database connection established.");

            // Format the input, and make a query for the card
            CardName = CardName.ToLower().Replace(' ', '_');
            string SQL = "SELECT * FROM `cards` WHERE name=@name";
            var command = new SQLiteCommand(SQL, connection);
            command.Parameters.AddWithValue("@name", CardName);

            // In order to be forward-compatible, we return a list (casted later to an array)
            // This ensures that additional seasons of cards require minimal maintenance on ARCore's part
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

        # region Paper_Thin_Sync_Wrappers
        /// <summary>
        /// See: Async version
        /// </summary>
        public CardsAPI GetPlayerInfo(string NationName) =>
            GetPlayerInfoAsync(NationName).GetAwaiter().GetResult();

        /// <summary>
        /// See: Async version
        /// </summary>
        public CardsAPI GetPlayerDeckInfo(string NationName) =>
            GetPlayerDeckInfoASync(NationName).GetAwaiter().GetResult();

        /// <summary>
        /// See: Async version
        /// </summary>
        public CardMarket GetCardMarket(string CardName, int Season) =>
            GetCardMarketAsync(CardName, Season).GetAwaiter().GetResult();

        /// <summary>
        /// See: Async version
        /// </summary>
        public Card GetCard(string CardName, int Season) =>
            GetCardAsync(CardName, Season).GetAwaiter().GetResult();

        /// <summary>
        /// See: Async version
        /// </summary>
        public Card[] GetCard(string CardName) =>
            GetCardAsync(CardName).GetAwaiter().GetResult();
        # endregion
    }
}
