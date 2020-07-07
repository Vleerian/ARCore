using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using ARCore.Core;
using ARCore.RealTimeTools;
using ARCore.DataDumpTools;
using ARCore.Types;
using ARCore.Helpers;

using Microsoft.Extensions.DependencyInjection;

namespace ARCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ARCore");
            Console.WriteLine("ARCore is provided AS-IS WITHOUT WARRANTY");
            Console.WriteLine("You are expected to have read and understood the");
            Console.WriteLine("NationStates API rules and rate-limits before use.");
            Console.WriteLine("The script author is not to be held liable for misuse");
            Console.WriteLine("of the program.");
            Console.WriteLine("https://www.nationstates.net/pages/api.html");
            Console.WriteLine("======================================================");

            Logger.logLevel = LogEventType.Information;
            new ARCoordinator("Atagait Denral", true)
                .Run(new Program().MainAsync);
        }

        private ARCards Cards;
        private APIHandler API;
        private ARData Data;
        private ARTelegrams Telegrams;

        public async Task MainAsync(IServiceProvider Services, CancellationToken cancellationToken){
            Cards = Services.GetRequiredService<ARCards>();
            API = Services.GetRequiredService<APIHandler>();
            Data = Services.GetRequiredService<ARData>();
            Telegrams = Services.GetRequiredService<ARTelegrams>();

            await Cards.InitializeDB();

            var PlayerInfo = (await Cards.GetPlayerInfoAsync("20XX")).PlayerInfo;
            var PlayerDeck = (await Cards.GetPlayerDeckInfoASync("20XX")).Deck
                .OrderBy(Card => Card.CardID)
                .GroupBy(Card => Card.CardID);

            Console.WriteLine($"Player {PlayerInfo.PlayerName}");
            Console.WriteLine($"Bank: {PlayerInfo.Bank}");

            foreach(var aCard in PlayerDeck)
            {
                Console.WriteLine("====================");
                var Card = aCard.First();
                var CD = await Cards.GetCardAsync(Card.CardID, Card.Season);
                Console.WriteLine($"{CD.Name} (S{CD.Season}) -- {CD.Rarity}");

                var MarketData = await Cards.GetCardMarketAsync(Card.CardID, Card.Season);
                if(MarketData.Markets.Count > 0)
                {
                    var buys = MarketData.Markets.Where(Market => Market.Type == "bid");
                    if(buys.Count() > 0)
                    {
                        var MaxBuy = buys.Max(Bid=>Bid.Price);
                        Console.WriteLine($"Buys: {buys.Count()} - MAX {MaxBuy}");
                    }
                    var sells = MarketData.Markets.Where(Market => Market.Type == "ask");
                    if(sells.Count() > 0)
                    {
                        var MinSell = sells.Min(Bid=>Bid.Price);
                        Console.WriteLine($"Sells: {sells.Count()} - MIN {MinSell}");
                    }
                }
            }
        }
    }
}
