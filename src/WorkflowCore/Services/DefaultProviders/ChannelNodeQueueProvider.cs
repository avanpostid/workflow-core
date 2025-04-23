using System;
using System.Collections.Concurrent;
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

        public Task QueueWork(string id, QueueType queue)
        {
            _queues[queue].Writer.TryWrite(id);
            return Task.CompletedTask;
        }

        public Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (_queues[queue].Reader.TryRead(out string id))
                return Task.FromResult(id);

            return Task.FromResult<string>(null);
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