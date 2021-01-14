using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;

namespace LagFreeScreenshots
{
    public class AwaitProvider
    {
        private readonly Queue<Action> myToMainThreadQueue = new Queue<Action>();

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
                    MelonLogger.LogWarning($"Exception if task: {ex}");
                }
            }
        }
        
        public YieldAwaitable Yield()
        {
            return new YieldAwaitable(myToMainThreadQueue);
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