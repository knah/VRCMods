using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;

namespace UIExpansionKit.API.Classes
{
    /// <summary>
    /// This class implements a simple delegate queue intended for interop with async/await
    /// </summary>
    public class AwaitProvider
    {
        private readonly Queue<Action> myToMainThreadQueue = new();
        /// <summary>
        /// The name of this queue specified in constructor; used in exception messages
        /// </summary>
        public readonly string QueueName;

        /// <summary>
        /// Creates a new queue
        /// </summary>
        /// <param name="queueName">Queue's name; used in exception messages</param>
        public AwaitProvider(string queueName)
        {
            QueueName = queueName;
        }

        /// <summary>
        /// Invokes all delegates currently in the queue.
        /// </summary>
        public void Flush()
        {
            List<Action> toProcess;

            if (myToMainThreadQueue.Count == 0)
                return;
            
            lock (myToMainThreadQueue)
            {
                toProcess = myToMainThreadQueue.ToList();
                myToMainThreadQueue.Clear();
            }

            foreach (var action in toProcess)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UiExpansionKitMod.Instance.Logger.Warning($"Exception in delegate queue {QueueName}: {ex}");
                }
            }
        }

        /// <summary>
        /// Adds an action to the queue. It will be invoked next time `Flush` is called.
        /// </summary>
        public void Add(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (myToMainThreadQueue)
                myToMainThreadQueue.Enqueue(action);
        }
        
        /// <summary>
        /// Returns an awaitable object (usable with `await` keyword). After it is awaited, async method execution execution will continue when `Flush` is invoked
        /// </summary>
        public YieldAwaitable Yield()
        {
            return new(myToMainThreadQueue);
        }

        public readonly struct YieldAwaitable : INotifyCompletion
        {
            private readonly Queue<Action> myToMainThreadQueue;

            public YieldAwaitable(Queue<Action> toMainThreadQueue)
            {
                myToMainThreadQueue = toMainThreadQueue;
            }

            public bool IsCompleted => false;

            public YieldAwaitable GetAwaiter() => this;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                lock (myToMainThreadQueue)
                    myToMainThreadQueue.Enqueue(continuation);
            }
        }
    }
}