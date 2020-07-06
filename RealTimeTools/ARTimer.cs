using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using ARCore.Core;
using ARCore.Helpers;
using ARCore.Types;

namespace ARCore.RealTimeTools
{
    public class ARTimer
    {
        Queue<double> AverageVariance;
        AsyncEvent<VarianceCalcEvent> VarianceCalc;

        public readonly double UpdateLength;
        public double UpdateStart;
        private long LastTimestamp;

        private readonly IServiceProvider _services;
        private readonly ARData _ARData;
        private readonly APIHandler _APIHandler;


        public const string HappeningsEndpoint = "http://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=change";

        public ARTimer(IServiceProvider services)
        {
            Logger.Log(LogEventType.Verbose, "Initializing ARTimer");
            
            _services = services;
            _ARData = services.GetRequiredService<ARData>();
            _APIHandler = services.GetRequiredService<APIHandler>();
            
            AverageVariance = new Queue<double>();
            VarianceCalc = new AsyncEvent<VarianceCalcEvent>();
            VarianceCalc.Register(CalculateVariance);

            LastTimestamp = 0;
            UpdateStart = 0;
        }

        /// <summary>
        /// Get the current world happenings and calculate variance based off it
        /// </summary>
        /// <param name="Major">True if the tool is being run at major update, false if not</param>
        public async Task GetHappening(bool Major)
        { 
            _APIHandler.Enqueue(out NSAPIRequest request, HappeningsEndpoint+$";sincetime={LastTimestamp}");
            var Happenings = await request.GetResultAsync<World>();
            LastTimestamp = Happenings.Happenings.First().Timestamp;
            foreach (var Happening in Happenings.Happenings)
            {
                if(Happening.Text.Contains("influence in") || Happening.Text.Contains("was ranked in the"))
                {
                    VarianceCalc?.InvokeAsync(this, new VarianceCalcEvent(Happening, Major));
                }
            }
        }

        /// <summary>
        /// Creates a variance estimate and pushes it to the averagevariance queue
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event</param>
        private Task CalculateVariance(object sender, VarianceCalcEvent e)
        {
            WorldEvent happening;
            Nation Nation;
            try
            {
                happening = e.Event;
                string NationName = happening.Text.Split("@@", StringSplitOptions.RemoveEmptyEntries)[0];
                Nation = _ARData.GetNation(NationName);
            }
            catch(Exception err)
            {
                Logger.Log(LogEventType.Error, "Nation not found.", err);
                return Task.CompletedTask;
            }

            //If no update start was set, set one.
            if(UpdateStart == 0)
            {
                //Use the happenings-provided timestamp, and zero it to the start of the update.
                //Since we filter for happenings that can only take place in the update, this operation
                //will not fire unless mid-update
                var tmp = HelpersStatic.UnixTimeStampToDateTime(happening.Timestamp);
                tmp = tmp.AddSeconds(-tmp.Second);
                tmp = tmp.AddMinutes(-tmp.Minute);
                tmp = tmp.AddMilliseconds(-tmp.Millisecond);
                if (tmp.Hour == 1 || tmp.Hour == 13)
                    tmp = tmp.AddHours(-1);
                //4 hours are subtracted so the displayed time represents time into the update
                tmp = tmp.AddHours(4);
                UpdateStart = (tmp - new DateTime(1970, 1, 1)).TotalSeconds;
            }

            //We calculate the variance by estimating when the nation updates, and subtracting it from actual update time
            double Actual = happening.Timestamp - UpdateStart;
            double Estimate = Nation.Index * _ARData.TimePerNation(e.Major);

            //VariancePerNation lets us extrapolate, which is more useful than the actual cumulative variance
            double VarianceActual = Actual - Estimate;
            double VariancePerNation = VarianceActual / Nation.Index;

            //I set the average variance queue size to 8. It's somewhat arbitrary, so I may add a configuration option for it later
            if (AverageVariance.Count > 8)
            {
                AverageVariance.Dequeue();
                AverageVariance.Enqueue(VariancePerNation);
            }
            else
            {
                //If our number of variance totals to work with is too low, we'll just push anything on.
                AverageVariance.Enqueue(VariancePerNation);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Estimates the update time + variance of a region, returned as seconds into the update.
        /// </summary>
        /// <param name="region">The target region</param>
        /// <returns>The estimated seconds into the update the target will update</returns>
        public double EstimateUpdate(string region, bool Major)
        {
            Region target;
            try
            {
                target = _ARData.GetRegion(region);
            }
            catch(Exception e)
            {
                Logger.Log(LogEventType.Error, "Region fetch error.", e);
                return 0.0;
            }
            
            if (target == null)
                return 0.0;
            var Nation = _ARData.GetNation(target.Nations[0]);
            if (AverageVariance.Count == 0)
                AverageVariance.Enqueue(1.0);
            double TotalVariance = Nation.Index * AverageVariance.Average();
            double Estimate = Nation.Index * _ARData.TimePerNation(Major);

            return TotalVariance + Estimate;
        }

        /// <summary>
        /// 'Bad' Estimate is the estimate purely using the time per nation calculated from the last update
        /// This estimate does not use variance at all.
        /// </summary>
        /// <param name="region">The target region</param>
        /// <returns></returns>
        public double BadEstimate(string region, bool Major)
        {
            var Region = _ARData.GetRegion(region);
            if (Region == null)
            {
                Logger.Log(LogEventType.Warning, $"Region {region} does not exist.");
                return 0.0;
            }
            else if(Region.Nations == null || Region.NumNations == 0)
            {
                Logger.Log(LogEventType.Warning, $"Region {region} has no nations.");
                return 0.0;
            }

            var Nation = _ARData.GetNation(Region.FirstNation);
            if(Nation == null)
            {
                Logger.Log(LogEventType.Warning, $"Nation {Region.FirstNation} not found in {region}.");
                return 0.0;
            }

            double Index = (double)Nation.Index;
            double TPN = _ARData.TimePerNation(Major);

            return Index * TPN;
        }
    }
}
