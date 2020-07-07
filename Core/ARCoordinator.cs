using System.Threading;
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
        public const string Verison = "0.4.0";

        private APIHandler API;
        private ARTimer Timer;
        private ARData Data;
        private ARTelegrams Telegrams;

        public readonly string User;
        public readonly bool Major;

        private CancellationTokenSource ShutdownToken;
        public readonly AsyncEvent On_Shutdown;

        public bool Running;

        public ARCoordinator(string user, bool major)
        {
            ShutdownToken = new CancellationTokenSource();
            Logger.Log(LogEventType.Verbose, "Initializing ARCoordinator");
            User = user;
            Major = major;
            On_Shutdown = new AsyncEvent();
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<APIHandler>()
                .AddSingleton<ARData>()
                .AddSingleton<ARTimer>()
                .AddSingleton<ARSheet>()
                .AddSingleton<ARCards>()
                .AddSingleton<ARTelegrams>()
                .BuildServiceProvider();
        }

        public async Task Shutdown()
        {
            Logger.Log(LogEventType.Information, "Shutdown requested.");
            Running = false;
            await On_Shutdown?.InvokeAsync(this);
        }

        // Wraps your async main and provides services
        public void Run(Func<IServiceProvider, CancellationToken, Task> MainCallback)
        {
            using(var services = ConfigureServices())
            {
                // Get references to all the services we'll need to configure
                API = services.GetRequiredService<APIHandler>();
                Timer = services.GetRequiredService<ARTimer>();
                Data = services.GetRequiredService<ARData>();
                Telegrams = services.GetRequiredService<ARTelegrams>();
                
                // Configure all our relevant services
                API.User = User;
                Data.User = User;

                // Boot up the API and Telegram loops
                API.APILoop(ShutdownToken.Token);
                Telegrams.TelegramLoop(ShutdownToken.Token);
                
                // Launch the Main callback
                MainCallback(services, ShutdownToken.Token)
                    .GetAwaiter().GetResult();

                // Once the main callback is complete, shut down
                ShutdownToken.Cancel();
            }
        }
    }
}
