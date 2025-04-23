using BenchmarkDotNet.Attributes;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace Benchmarks;

[MemoryDiagnoser]
public class QueueProvidersBenchmarks
{
    [Benchmark]
    public async Task ChannelNodeQueueProvider_Benchmark()
    {
        await BenchmarkInternal(new ChannelNodeQueueProvider());
    }
    
    [Benchmark]
    public async Task SingleNodeQueueProvider_Benchmark()
    {
        await BenchmarkInternal(new SingleNodeQueueProvider());
    }

    private async Task BenchmarkInternal(IQueueProvider queueProvider)
    {
        RunQueueWorkTask(queueProvider, 1_000_000).Wait();
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        RunQueueWorkTask(queueProvider, 100000);
        
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