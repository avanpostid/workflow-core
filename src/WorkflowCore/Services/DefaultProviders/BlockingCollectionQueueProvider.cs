using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class BlockingCollectionQueueProvider : IQueueProvider
    {
        private readonly Dictionary<QueueType, BlockingCollection<string>> _queues = new Dictionary<QueueType, BlockingCollection<string>>()
        {
            [QueueType.Workflow] = new BlockingCollection<string>(),
            [QueueType.Event] = new BlockingCollection<string>(),
            [QueueType.Index] = new BlockingCollection<string>()
        };

        public bool IsDequeueBlocking => true;

        public ValueTask QueueWork(string id, QueueType queue)
        {
            _queues[queue].Add(id);
            return default;
        }

        public ValueTask<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            return _queues[queue].TryTake(out var id, 100, cancellationToken) 
                ? new ValueTask<string>(id) 
                : new ValueTask<string>((string)null);
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {            
        }
        
    }
}
