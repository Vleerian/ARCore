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

        public VarianceCalcEvent(WorldEvent aEvent) =>
            Event = aEvent;

        public static implicit operator VarianceCalcEvent(WorldEvent e) =>
            new VarianceCalcEvent(e);
    }
}
