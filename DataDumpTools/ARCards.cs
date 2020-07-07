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
            string SQL = "INSERT INTO `cards` (cardID, season, name, type, motto, category, region, cardcategory) VALUES (@cardID, @season, @name, @type, @motto, @category, @region, @cardcategory);";
            var Command = new SQLiteCommand(SQL, connection);
            Command.Parameters.AddWithValue("@cardID", card.ID);
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
        /// A wrapper to clean up initializing cards with DbDataReader
        /// </summary>
        Card ReadCard(DbDataReader dbReader) => new Card() {
            ID       = (int)dbReader.GetInt64(1),
            Season   = (int)dbReader.GetInt64(2),
            name     = dbReader.GetString(3),
            Type     = dbReader.GetString(4),
            Motto    = dbReader.GetString(5),
            Category = dbReader.GetString(6),
            Region   = dbReader.GetString(7),
            Rarity   = dbReader.GetString(8)
        };

        /// <summary>
        /// A wrapper to clean up fetching single cards from the database
        /// </summary>
        /// <param name="CardFetchCommand"></param>
        /// <returns></returns>
        async Task<Card> FetchOneCard(SQLiteCommand CardFetchCommand)
        {
            using(var dbReader = await CardFetchCommand.ExecuteReaderAsync())
            {
                while(dbReader.Read())
                {
                    return ReadCard(dbReader);
                }
            }
            return null;
        }

        /// <summary>
        /// A wrapper to clean up fetching multiple cards from the database
        /// </summary>
        /// <param name="CardFetchCommand"></param>
        /// <returns></returns>
        async Task<Card[]> FetchCards(SQLiteCommand CardFetchCommand)
        {
            List<Card> cards = new List<Card>();
            using(var dbReader = await CardFetchCommand.ExecuteReaderAsync())
            {
                while(dbReader.Read())
                {
                    cards.Add(ReadCard(dbReader));
                }
            }
            return cards.ToArray();
        }

        /// <summary>
        /// Shorthand method to retrieve player cards information
        /// </summary>
        /// <param name="NationName">The nation you want cards info for</param>
        public async Task<CardsPlayerInfo> GetPlayerInfoAsync(string NationName)
        {
            API.Enqueue(out NSAPIRequest Request, PlayerInfoEnd+NationName);
            return await Request.GetResultAsync<CardsPlayerInfo>();
        }

        /// <summary>
        /// Shorthand method to retrieve player deck information
        /// </summary>
        /// <param name="NationName">The nation you want deck info for</param>
        public async Task<CardsDeckInfo> GetPlayerDeckInfoASync(string NationName)
        {
            API.Enqueue(out NSAPIRequest Request, PlayerDeckEnd+NationName);
            return await Request.GetResultAsync<CardsDeckInfo>();
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
            return await GetCardMarketAsync(Card.ID, Season);
        }

        /// <summary>
        /// Shorthand method to retrieve market info for a card
        /// </summary>
        /// <param name="CardID">The ID of the card you want info about</param>
        /// <param name="Season">The season that card is in</param>
        /// <returns></returns>
        public async Task<CardMarket> GetCardMarketAsync(int CardID, int Season)
        {
            API.Enqueue(out NSAPIRequest Request, CardMarketEnd+$"cardid={CardID};season={Season}");
            return await Request.GetResultAsync<CardMarket>();
        }

        /// <summary>
        /// Retrieves card information from the database, limited to one season
        /// </summary>
        /// <param name="CardName">The name of the card you want information about</param>
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

            return await FetchOneCard(command);
        }

        /// <summary>
        /// Retrieves card information from the database, limited to one season
        /// </summary>
        /// <param name="CardName">The ID of the card you want information about</param>
        /// <param name="Season">What season to look for</param>
        /// <returns>The specific card retrieved, or null if none found</returns>
        public async Task<Card> GetCardAsync(int CardID, int Season)
        {
            if(connection == null)
                throw new ApplicationException("No database connection established.");

            string SQL = "SELECT * FROM `cards` WHERE cardID=@cardID AND season=@season;";
            var command = new SQLiteCommand(SQL, connection);
            command.Parameters.AddWithValue("@cardID", CardID);
            command.Parameters.AddWithValue("@season", Season);

            return await FetchOneCard(command);
        }

        /// <summary>
        /// Retrieves card information from the database
        /// </summary>
        /// <param name="CardName">The name of the card you want information about</param>
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

            return await FetchCards(command);
        }

        /// <summary>
        /// Retrieves card information from the database
        /// </summary>
        /// <param name="CardID">The ID of the card you want information about</param>
        /// <param name="Season">What season to look for</param>
        /// <returns>An array of retrieved cards, an empty array if none are found</returns>
        public async Task<Card[]> GetCardAsync(int CardID)
        {
            if(connection == null)
                throw new ApplicationException("No database connection established.");

            string SQL = "SELECT * FROM `cards` WHERE cardID=@cardID";
            var command = new SQLiteCommand(SQL, connection);
            command.Parameters.AddWithValue("@cardID", CardID);

            return await FetchCards(command);
        }

        # region Paper_Thin_Sync_Wrappers
        /// <summary>
        /// See: <see cref="GetPlayerInfoAsync(string NationName)"/>
        /// </summary>
        public CardsPlayerInfo GetPlayerInfo(string NationName) =>
            GetPlayerInfoAsync(NationName).GetAwaiter().GetResult();

        /// <summary>
        /// See: <see cref="GetPlayerDeckInfoASync(string NationName)"/>
        /// </summary>
        public CardsDeckInfo GetPlayerDeckInfo(string NationName) =>
            GetPlayerDeckInfoASync(NationName).GetAwaiter().GetResult();

        /// <summary>
        /// See: <see cref="GetCardMarketAsync(string CardName, int Season)"/>
        /// </summary>
        public CardMarket GetCardMarket(string CardName, int Season) =>
            GetCardMarketAsync(CardName, Season).GetAwaiter().GetResult();

        /// <summary>
        /// See: <see cref="GetCardAsync(string CardName, int Season)"/>
        /// </summary>
        public Card GetCard(string CardName, int Season) =>
            GetCardAsync(CardName, Season).GetAwaiter().GetResult();

        /// <summary>
        /// See: <see cref="GetCardAsync(int CardID, int Season)"/>
        /// </summary>
        public Card GetCard(int CardID, int Season) =>
            GetCardAsync(CardID, Season).GetAwaiter().GetResult();

        /// <summary>
        /// See: <see cref="GetCardAsync(string CardName) "/>
        /// </summary>
        public Card[] GetCard(string CardName) =>
            GetCardAsync(CardName).GetAwaiter().GetResult();

        /// <summary>
        /// See: <see cref="GetCardAsync(int CardID)"/>
        /// </summary>
        public Card[] GetCard(int CardID) =>
            GetCardAsync(CardID).GetAwaiter().GetResult();
        # endregion
    }
}
