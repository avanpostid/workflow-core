using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using Xunit;

namespace WorkflowCore.UnitTests.Services;

public class QueueProvidersTests
{
    [Fact]
    public async Task ChannelNodeQueueProvider_ForDotTrace()
    {
        var queueProvider = new ChannelNodeQueueProvider();
        
        RunQueueWorkTask(queueProvider, 10_000_000).Wait();
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        
        var dequeueWork = await queueProvider.DequeueWork(QueueType.Event, CancellationToken.None);
        while (dequeueWork is not null)
        {
            dequeueWork = await queueProvider.DequeueWork(QueueType.Event, CancellationToken.None);
        }
    }
    
    [Fact]
    public async Task BlockingCollectionQueueProvider_ForDotTrace()
    {
        var queueProvider = new BlockingCollectionQueueProvider();
        
        RunQueueWorkTask(queueProvider, 10_000_000).Wait();
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        RunQueueWorkTask(queueProvider, 10000);
        
        var dequeueWork = await queueProvider.DequeueWork(QueueType.Event, CancellationToken.None);
        while (dequeueWork is not null)
        {
            dequeueWork = await queueProvider.DequeueWork(QueueType.Event, CancellationToken.None);
        }
    }
    
    private Task RunQueueWorkTask(IQueueProvider queueProvider, int count)
    {
        return Task.Run(async () =>
        {
            var guids = Enumerable.Range(1, count).Select(_ => Guid.NewGuid()).ToList();
            foreach (var guid in guids)
            {
                await queueProvider.QueueWork(guid.ToString(), QueueType.Event);
            }
        });
    }
}