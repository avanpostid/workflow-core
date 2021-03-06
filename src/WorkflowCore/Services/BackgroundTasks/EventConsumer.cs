﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal class EventConsumer : QueueConsumer, IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IDateTimeProvider _datetimeProvider;
        protected override int MaxConcurrentItems => 2;
        protected override QueueType Queue => QueueType.Event;

        public EventConsumer(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options, IDateTimeProvider datetimeProvider)
            : base(queueProvider, loggerFactory, options)
        {
            _persistenceStore = persistenceStore;
            _lockProvider = lockProvider;
            _datetimeProvider = datetimeProvider;
        }

        protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
        {
            if (!await _lockProvider.AcquireLock($"evt:{itemId}", cancellationToken))
            {
                Logger.LogInformation($"Event locked {itemId}");
                return;
            }
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var evt = await _persistenceStore.GetEvent(itemId);
                if (evt.EventTime <= _datetimeProvider.Now.ToUniversalTime())
                {
                    var subs = await _persistenceStore.GetSubcriptions(evt.EventName, evt.EventKey, evt.EventTime);
                    var toQueue = new List<string>();
                    var complete = true;

                    foreach (var sub in subs.ToList())
                        complete = complete && await SeedSubscription(evt, sub, toQueue, cancellationToken);

                    if (complete)
                        await _persistenceStore.MarkEventProcessed(itemId);

                    foreach (var eventId in toQueue)
                        await QueueProvider.QueueWork(eventId, QueueType.Event);
                }
            }
            finally
            {
                await _lockProvider.ReleaseLock($"evt:{itemId}");
            }
        }
        
        private async Task<bool> SeedSubscription(Event evt, EventSubscription sub, List<string> toQueue, CancellationToken cancellationToken)
        {            
            foreach (var eventId in await _persistenceStore.GetEvents(sub.EventName, sub.EventKey, sub.SubscribeAsOf))
            {
                if (eventId == evt.Id)
                    continue;

                var siblingEvent = await _persistenceStore.GetEvent(eventId);
                if ((!siblingEvent.IsProcessed) && (siblingEvent.EventTime < evt.EventTime))
                {
                    await QueueProvider.QueueWork(eventId, QueueType.Event);
                    return false;
                }

                if (!siblingEvent.IsProcessed)
                    toQueue.Add(siblingEvent.Id);
            }

            if (!await _lockProvider.AcquireLock(sub.WorkflowId, cancellationToken))
            {
                Logger.LogInformation("Workflow locked {0}", sub.WorkflowId);
                return false;
            }
            
            try
            {
                var workflow = await _persistenceStore.GetWorkflowInstance(sub.WorkflowId);
                var pointers = workflow.ExecutionPointers.Where(p => p.EventKey == sub.EventKey && p.EndTime == null).ToList();
                if (pointers.Any(x => x.EventPublished))
                {
                    return false;
                }

                foreach (var p in pointers)
                {
                    p.EventData = evt.EventData;
                    p.EventPublished = true;
                    p.Active = true;
                }
                workflow.NextExecution = 0;
                await _persistenceStore.PersistWorkflow(workflow);                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                return false;
            }
            finally
            {
                await _lockProvider.ReleaseLock(sub.WorkflowId);
                await QueueProvider.QueueWork(sub.WorkflowId, QueueType.Workflow);
            }
        }
    }
}
