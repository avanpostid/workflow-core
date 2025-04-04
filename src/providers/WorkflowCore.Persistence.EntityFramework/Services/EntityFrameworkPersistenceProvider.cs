using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Interfaces;

namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public class EntityFrameworkPersistenceProvider : IPersistenceProvider
    {
        private readonly bool _canCreateDB;
        private readonly bool _canMigrateDB;
        private readonly IWorkflowDbContextFactory _contextFactory;

        public EntityFrameworkPersistenceProvider(IWorkflowDbContextFactory contextFactory, bool canCreateDB, bool canMigrateDB)
        {
            _contextFactory = contextFactory;
            _canCreateDB = canCreateDB;
            _canMigrateDB = canMigrateDB;
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            await using var db = ConstructDbContext();
            subscription.Id = Guid.NewGuid().ToString();
            var persistable = subscription.ToPersistable();
            db.Set<PersistedSubscription>().Add(persistable);
            await db.SaveChangesAsync();
            return subscription.Id;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            await using var db = ConstructDbContext();
            workflow.Id = Guid.NewGuid().ToString();
            var persistable = workflow.ToPersistable();
            db.Set<PersistedWorkflow>().Add(persistable);
            await db.SaveChangesAsync();
            return workflow.Id;
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            await using var db = ConstructDbContext();
            var now = asAt.ToUniversalTime().Ticks;
            var raw = await db.Set<PersistedWorkflow>()
                .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                .Select(x => x.InstanceId)
                .ToArrayAsync();

            return raw.Select(s => s.ToString()).ToArray();
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            await using (var db = ConstructDbContext())
            {
                var query = db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(x => x.Status == status.Value);

                if (!String.IsNullOrEmpty(type))
                    query = query.Where(x => x.WorkflowDefinitionId == type);

                if (createdFrom.HasValue)
                    query = query.Where(x => x.CreateTime >= createdFrom.Value);

                if (createdTo.HasValue)
                    query = query.Where(x => x.CreateTime <= createdTo.Value);

                var rawResult = await query.Skip(skip).Take(take).ToArrayAsync();

                return rawResult.Select(item => item.ToWorkflowInstance()).ToArray();
            }
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            await using var db = ConstructDbContext();
            var uid = new Guid(Id);
            var raw = await db.Set<PersistedWorkflow>()
                .Include(wf => wf.ExecutionPointers)
                .ThenInclude(ep => ep.ExtensionAttributes)
                .Include(wf => wf.ExecutionPointers)
                .FirstAsync(x => x.InstanceId == uid);

            if (raw == null)
                return null;

            return raw.ToWorkflowInstance();
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return Enumerable.Empty<WorkflowInstance>();
            }

            await using var db = ConstructDbContext();
            var uids = ids.Select(i => new Guid(i));
            var raw = await db.Set<PersistedWorkflow>()
                .Include(wf => wf.ExecutionPointers)
                .ThenInclude(ep => ep.ExtensionAttributes)
                .Include(wf => wf.ExecutionPointers)
                .Where(x => uids.Contains(x.InstanceId))
                .ToArrayAsync();

            return raw.Select(i => i.ToWorkflowInstance());
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            await using var db = ConstructDbContext();
            var uid = new Guid(workflow.Id);
            var existingEntity = await db.Set<PersistedWorkflow>()
                .Where(x => x.InstanceId == uid)
                .Include(wf => wf.ExecutionPointers)
                .ThenInclude(ep => ep.ExtensionAttributes)
                .Include(wf => wf.ExecutionPointers)
                .AsTracking()
                .FirstAsync();

            workflow.ToPersistable(existingEntity);
            await db.SaveChangesAsync();
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            await using (var db = ConstructDbContext())
            {
                var uid = new Guid(eventSubscriptionId);
                var existing = await db.Set<PersistedSubscription>().FirstAsync(x => x.SubscriptionId == uid);
                db.Set<PersistedSubscription>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }

        public virtual void EnsureStoreExists()
        {
            using (var context = ConstructDbContext())
            {
                if (_canCreateDB && !_canMigrateDB)
                {
                    context.Database.EnsureCreated();
                    return;
                }

                if (_canMigrateDB)
                {
                    context.Database.Migrate();
                    return;
                }
            }
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey, DateTime asOf)
        {
            await using var db = ConstructDbContext();
            asOf = asOf.ToUniversalTime();
            var query = db.Set<PersistedSubscription>()
                .Where(x => x.EventKey == eventKey && x.SubscribeAsOf <= asOf).AsQueryable();
            if (!string.IsNullOrEmpty(eventName))
            {
                query = query.Where(x => x.EventName == eventName);
            }

            var raw = await query.ToArrayAsync();
            return raw.Select(item => item.ToEventSubscription()).ToArray();
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            await using var db = ConstructDbContext();
            newEvent.Id = Guid.NewGuid().ToString();
            var persistable = newEvent.ToPersistable();
            db.Set<PersistedEvent>().Add(persistable);
            await db.SaveChangesAsync();
            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id)
        {
            var uid = new Guid(id);
            await using var db = ConstructDbContext();
            var raw = await db.Set<PersistedEvent>()
                .FirstAsync(x => x.EventId == uid);

            return raw?.ToEvent();
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var now = asAt.ToUniversalTime();
            await using var db = ConstructDbContext();
            asAt = asAt.ToUniversalTime();
            var raw = await db.Set<PersistedEvent>()
                .Where(x => !x.IsProcessed)
                .Where(x => x.EventTime <= now)
                .Select(x => x.EventId)
                .ToArrayAsync();

            return raw.Select(s => s.ToString()).ToArray();
        }

        public async Task MarkEventProcessed(string id)
        {
            await using var db = ConstructDbContext();
            var uid = new Guid(id);
            var existingEntity = await db.Set<PersistedEvent>()
                .Where(x => x.EventId == uid)
                .AsTracking()
                .FirstAsync();

            existingEntity.IsProcessed = true;
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            await using var db = ConstructDbContext();
            var raw = await db.Set<PersistedEvent>()
                .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                .Where(x => x.EventTime >= asOf)
                .Select(x => x.EventId)
                .ToArrayAsync();

            return raw.Select(s => s.ToString()).ToArray();
        }

        public async Task MarkEventUnprocessed(string id)
        {
            await using var db = ConstructDbContext();
            var uid = new Guid(id);
            var existingEntity = await db.Set<PersistedEvent>()
                .Where(x => x.EventId == uid)
                .AsTracking()
                .FirstAsync();

            existingEntity.IsProcessed = false;
            await db.SaveChangesAsync();
        }

        public async Task RemoveEventsByKey(string eventKey)
        {
            await using var db = ConstructDbContext();
            var rowsToDelete = await db.Set<PersistedEvent>().Where(x => x.EventKey == eventKey).ToArrayAsync();
            if (rowsToDelete.Any())
            {
                db.Set<PersistedEvent>().RemoveRange(rowsToDelete);
            }

            await db.SaveChangesAsync();
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            await using var db = ConstructDbContext();
            var executionErrors = errors as ExecutionError[] ?? errors.ToArray();
            if (executionErrors.Any())
            {
                foreach (var error in executionErrors)
                {
                    db.Set<PersistedExecutionError>().Add(error.ToPersistable());
                }
                
                await db.SaveChangesAsync();
            }
        }

        private WorkflowDbContext ConstructDbContext()
        {
            return _contextFactory.Build();
        }

    }
}
