using System;
using System.Collections.Generic;
using System.Text;

using ARCore.Types;

namespace ARCore.Core
{
    public class AsyncEventArgs
    {
        public bool Handled { get; set; }
    }

    public class VarianceCalcEvent : AsyncEventArgs
    {
        public WorldEvent Event;
        public bool Major;

        public VarianceCalcEvent(WorldEvent aEvent, bool aMajor)
        {
            Event = aEvent;
            Major = aMajor;
        }

        /// <summary>
        /// Converts a WorldEvent object to a VarianceCalcEvent
        /// NOTE: By default, the event will be assumed to take place at Major Update
        /// </summary>
        /// <param name="e">The Event</param>
        public static implicit operator VarianceCalcEvent(WorldEvent e) =>
            new VarianceCalcEvent(e, true);
    }
}
