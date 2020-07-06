using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections.Generic;

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
        
        List<List<Card>> Cards;

        public ARCards(IServiceProvider services){
            Services = services;
            Data = Services.GetRequiredService<ARData>();
            API = Services.GetRequiredService<APIHandler>();

            // Once this uses a database, this terrible code will be gone
            // But in the meantime, this works.

            Logger.Log(LogEventType.Debug, "Parsing Cardset 1");
            var CardSet_1 = APIHandler.ParseDataDump<CardsDataDump>("cardlist_S1.xml.gz");
            Logger.Log(LogEventType.Debug, "Parsing Cardset 2");
            var CardSet_2 = APIHandler.ParseDataDump<CardsDataDump>("cardlist_S2.xml.gz");

            Cards = new List<List<Card>>(){
                CardSet_1.CardSet.Cards,
                CardSet_2.CardSet.Cards
            };
        }

        const string CardsEndpoint = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+deck+info;nationname=";
        public async Task<CardsAPI> GetPlayerInfo(string NationName)
        {
            API.Enqueue(out NSAPIRequest Request, CardsEndpoint+NationName);
            return await Request.GetResultAsync<CardsAPI>();
        }

        public async Task<Card> GetCard(string CardName, int Season)
        {
            await Task.CompletedTask;
            Season--;
            if(Season > Cards.Count || Season < 1)
                throw new ArgumentException("Invalid season.");
            return Cards[Season]
                .Where(Card=>Card.Name == CardName)
                .FirstOrDefault();
        }

        public async Task<CardPair> GetCard(string CardName)
        {
            await Task.CompletedTask;
            CardName = CardName.ToLower().Replace(" ","_");
            return new CardPair(){
                Season1 = Cards[0]
                    .Where(Card=>Card.Name == CardName)
                    .FirstOrDefault(),
                Season2 = Cards[1]
                    .Where(Card=>Card.Name == CardName)
                    .FirstOrDefault()
            };
        }
    }
}
