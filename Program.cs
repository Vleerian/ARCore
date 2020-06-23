using System;
using System.Linq;
using System.Threading.Tasks;

using ARCore.Core;
using ARCore.RealTimeTools;
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
            Console.WriteLine("ARCore-20XX");
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

            return;

            /*
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                switch(options.Mode)
                {
                    case "verbose":
                        Logger.logLevel = LogEventType.Verbose; break;
                    case "debug":
                        Logger.logLevel = LogEventType.Debug; break;
                    case "info":
                    default:
                        Logger.logLevel = LogEventType.Information; break;
                    case "error":
                        Logger.logLevel = LogEventType.Warning; break;
                    case "none":
                        Logger.logLevel = LogEventType.None; break;
                }

                switch(options.Update.ToLower()){
                    case "minor":
                        Major = false; break;
                    case "major":
                        Major = true; break;
                    default:
                        Logger.Log(LogEventType.Fatal, "Invalid update. Only \"major\" and \"minor\" are accepted."); return;
                }
                new ARCoordinator(options.User, options.NDataDump, options.RDataDump).Run(new Program().MainAsync);
            });
            */
        }

        private APIHandler API;
        private ARTimer Timer;
        private ARData Data;

        public async Task MainAsync(IServiceProvider Services){
            API = Services.GetRequiredService<APIHandler>();
            Timer = Services.GetRequiredService<ARTimer>();
            Data = Services.GetRequiredService<ARData>();

            var n = Data.GetNation("chopaka");
            Console.WriteLine(n.Name);
            var r = Data.GetRegion(n.Region);
            Console.WriteLine(r.name);

            await Task.CompletedTask;
        }
    }
}
