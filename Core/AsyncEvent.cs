using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCore.Core
{
    public delegate Task AsyncEventHandler(object Sender);
    public delegate Task AsyncEventHandler<T>(object Sender, T e);

    public class AsyncEvent
    {
        private List<AsyncEventHandler> Handlers;

        public AsyncEvent() =>
            Handlers = new List<AsyncEventHandler>();

        public async Task InvokeAsync(object Sender)
        {
            if (!Handlers.Any())
                return;
            foreach (var handler in Handlers)
            {
                await handler(Sender).ConfigureAwait(false);
            }
        }

        public void Register(AsyncEventHandler aTask)
        {
            Handlers.Add(aTask);
        }

        public void Unregister(AsyncEventHandler aTask)
        {
            Handlers.Remove(aTask);
        }

    }

    public class AsyncEvent<T>
    {
        private List<AsyncEventHandler<T>> Handlers;

        public AsyncEvent() =>
            Handlers = new List<AsyncEventHandler<T>>();

        public async Task InvokeAsync(object Sender, T e)
        {
            if (!Handlers.Any())
                return;
            foreach (var handler in Handlers)
            {
                await handler(Sender, e).ConfigureAwait(false);
            }
        }

        public void Register(AsyncEventHandler<T> aTask)
        {
            Handlers.Add(aTask);
        }

        public void Unregister(AsyncEventHandler<T> aTask)
        {
            Handlers.Remove(aTask);
        }

    }
}
