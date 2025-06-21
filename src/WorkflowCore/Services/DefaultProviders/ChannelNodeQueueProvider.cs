using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class ChannelNodeQueueProvider : IQueueProvider
    {
        private readonly Dictionary<QueueType, Channel<string>> _queues = new Dictionary<QueueType, Channel<string>>()
        {
            [QueueType.Workflow] = Channel.CreateUnbounded<string>(),
            [QueueType.Event] = Channel.CreateUnbounded<string>(),
            [QueueType.Index] = Channel.CreateUnbounded<string>()
        };

        public bool IsDequeueBlocking => true;

        public ValueTask QueueWork(string id, QueueType queue)
        {
            _queues[queue].Writer.TryWrite(id);
            return default;
        }

        public ValueTask<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            return _queues[queue].Reader.TryRead(out var id) 
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