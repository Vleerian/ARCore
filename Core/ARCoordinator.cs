using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using Microsoft.Extensions.DependencyInjection;

using ARCore.Types;
using ARCore.Helpers;
using ARCore.RealTimeTools;
using ARCore.DataDumpTools;

using Newtonsoft.Json;

namespace ARCore.Core
{
    /// <summary>
    /// ARCoordinator is the topmost class that coordinates all the other modules.
    /// It is also what handles all the resources.
    /// </summary>
    public partial class ARCoordinator
    {
        private static int MajorVersion = 0;
        private static int MinorVersion = 2;
        private static int PatchNumber = 0;
        public static string Verison => $"{MajorVersion}.{MinorVersion}.{PatchNumber}";

        private APIHandler API;
        private ARTimer Timer;
        private ARData Data;
        public readonly string User;

        public readonly AsyncEvent On_Shutdown;

        public bool Running;

        public ARCoordinator(string user, string NationDump, string RegionDump)
        {
            Logger.Log(LogEventType.Verbose, "Initializing ARCoordinator");
            User = user;
            On_Shutdown = new AsyncEvent();
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<APIHandler>()
                .AddSingleton<ARData>()
                .AddSingleton<ARTimer>()
                .AddSingleton<ARSheet>()
                .BuildServiceProvider();
        }

        public async Task Shutdown()
        {
            Logger.Log(LogEventType.Information, "Shutdown requested.");
            Running = false;
            await On_Shutdown?.InvokeAsync(this);
        }

        // Wraps your async main and provides services
        public void Run(Func<IServiceProvider, Task> MainCallback)
        {
            Logger.logLevel = LogEventType.Debug;

            using(var services = ConfigureServices())
            {
                API = services.GetRequiredService<APIHandler>();
                Timer = services.GetRequiredService<ARTimer>();
                Data = services.GetRequiredService<ARData>();
                
                API.User = User;
                Data.User = User;

                On_Shutdown.Register(API.Shutdown);
                On_Shutdown.Register(Timer.Shutdown);

                MainCallback(services)
                    .GetAwaiter().GetResult();                
            }
        }
    }
}
