using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public class TaskQueue<T>
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<Func<Task<T>>> _queue = new ConcurrentQueue<Func<Task<T>>>();

        public TaskQueue(int maxConcurrentTasks)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
        }

        public async Task<T> Queue(Func<Task<T>> taskGenerator)
        {
            _queue.Enqueue(taskGenerator);
            await _semaphore.WaitAsync();

            return await DequeueAndRun();
        }

        private async Task<T> DequeueAndRun()
        {
            if (_queue.TryDequeue(out Func<Task<T>> taskGenerator))
            {
                try
                {
                    return await taskGenerator();
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            else
            {
                throw new InvalidOperationException("Queue is empty.");
            }
        }
    }
}
