using System.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using ARCore.Core;
using ARCore.Helpers;

namespace ARCore
{
    class ARTelegrams
    {
        public string ClientKey;

        public const int RecruitmentLimit = 180000;
        public const int NonRecruitmentLimit = 30000;

        private DateTime NextTelegramTime;

        private Queue<TelegramRequest> TelegramQueue;

        public bool RecruitNew;
        public bool RecruitFromSinkers;

        public IServiceProvider Services;
        private APIHandler API;

        public ARTelegrams(IServiceProvider services)
        {
            API = services.GetRequiredService<APIHandler>();
            Services = services;
            ClientKey = string.Empty;

            RecruitNew = false;
            RecruitFromSinkers = false;

            NextTelegramTime = DateTime.Now.AddSeconds(180);
            TelegramQueue = new Queue<TelegramRequest>();
        }

        public void EnqueueRecruitment(string recipient, string templateID, string secretKey) =>
            TelegramQueue.Enqueue(new TelegramRequest(TelegramType.Recruitment, recipient, templateID, ClientKey, secretKey));

        public void EnqueueNonRecruitment(string recipient, string templateID, string secretKey) =>
            TelegramQueue.Enqueue(new TelegramRequest(TelegramType.NonRecruitment, recipient, templateID, ClientKey, secretKey));

        public void SendTask(string Result)
        {
            if (Result.ToLower().Contains("queued"))
                Logger.Log(LogEventType.Information, "Queued successfully.");
            else
                Logger.Log(LogEventType.Error, "Failed to queue message.");
        }

        public async void TelegramLoop(CancellationToken cancellationToken)
        {
            // Start with the longest TG delay to ensure rate limit compliance
            await Task.Delay(RecruitmentLimit);
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait until the next telegram can be set
                while (DateTime.Now < NextTelegramTime)
                {
                    await Task.Delay(1000);
                    // If cancellation was requested, break out
                    if(cancellationToken.IsCancellationRequested)
                        break;
                }

                if(TelegramQueue.Count > 0)
                {
                    if(ClientKey == string.Empty)
                        throw new ApplicationException("No client key supplied.");
                    TelegramRequest tg = TelegramQueue.Dequeue();
                    if (tg.Type == TelegramType.Recruitment)
                        NextTelegramTime = DateTime.Now.AddMilliseconds(RecruitmentLimit);
                    else
                        NextTelegramTime = DateTime.Now.AddMilliseconds(NonRecruitmentLimit);
                    API.Enqueue(out NSAPIRequest _, tg.Uri);
                    Logger.Log(LogEventType.Information, $"Attempting to queue telegram for {tg.Recipient}");
                }
            }
        }
    }
}
