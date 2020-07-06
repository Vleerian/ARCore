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
        public static bool Major;

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

            Program.Major = true;
            Logger.logLevel = LogEventType.Verbose;
            new ARCoordinator("Atagait Denral", "nations.xml.gz", "regions.xml.gz").Run(new Program().MainAsync);
        }

        private ARCards Cards;
        private APIHandler API;
        private ARData Data;
        private ARTelegrams Telegrams;

        public async Task MainAsync(IServiceProvider Services){
            Cards = Services.GetRequiredService<ARCards>();
            API = Services.GetRequiredService<APIHandler>();
            Data = Services.GetRequiredService<ARData>();
            Telegrams = Services.GetRequiredService<ARTelegrams>();
        }
    }
}
