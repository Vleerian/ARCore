using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;

using ARCore.Core;
using ARCore.RealTimeTools;
using ARCore.DataDumpTools;
using ARCore.Types;
using ARCore.Helpers;

using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace ARCore
{
    class Program
    {
        public class Options
        {
            [Option('n', "nation_dump", Required = false, Default = "nations.xml.gz", HelpText ="The nation data dump file to use.")]
            public string NDataDump { get; set; }
            [Option('r', "region_dump", Required = false, Default = "regions.xml.gz", HelpText = "The region data dump file to use.")]
            public string RDataDump { get; set; }
            [Option('m', "mode", Required = false, Default = "info", HelpText = "Output message mode.")]
            public string Mode { get; set; }
            [Option('u', "user", Required = true, Default = "Atagait Denral", HelpText = "The user of the application.")]
            public string User { get; set; }
            [Option('t', "update", Required = true, HelpText = "Which update you are targeting for (major/minor)")]
            public string Update {get; set; }
        }
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

            Logger.logLevel = LogEventType.Verbose;
            new ARCoordinator("Atagait Denral", true)
                .Run(new Program().MainAsync);
        }

        private ARCards Cards;
        private APIHandler API;
        private ARData Data;
        private ARTelegrams Telegrams;

        const string PlayerInfoEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+info;nationname=";
        const string PlayerDeckEnd = "https://www.nationstates.net/cgi-bin/api.cgi?q=cards+deck;nationname=";

        public async Task MainAsync(IServiceProvider Services, CancellationToken cancellationToken){
            Cards = Services.GetRequiredService<ARCards>();
            API = Services.GetRequiredService<APIHandler>();
            Data = Services.GetRequiredService<ARData>();
            Telegrams = Services.GetRequiredService<ARTelegrams>();

            API.Enqueue(out NSAPIRequest PlayerInfo, PlayerInfoEnd + "20XX");
            API.Enqueue(out NSAPIRequest PlayerDeck, PlayerDeckEnd + "20XX");
            while(!PlayerInfo.Done && !PlayerDeck.Done);

            var Player = await PlayerInfo.GetResultAsync<CardsAPI>();
            var Deck = await PlayerInfo.GetResultAsync<CardMarket>();

            Console.WriteLine("Done...");
        }
    }
}
