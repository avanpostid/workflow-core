using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.SqlServer
{
    public class SqlServerContext : WorkflowDbContext
    {
        private readonly string _connectionString;
        private readonly string _tablePrefix;
        private readonly string _schema;

        public SqlServerContext(string connectionString, string schema, string tablePrefix)
            : base()
        {
            if (!connectionString.Contains("MultipleActiveResultSets"))
                connectionString += ";MultipleActiveResultSets=True";
            _schema = schema;
            _tablePrefix = tablePrefix;
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(_connectionString, options =>
                options.MigrationsHistoryTable(MigrationMetaInfo.MigrationTableName,
                    MigrationMetaInfo.DbSchema));
        }

        protected override void ConfigureSubscriptionStorage(EntityTypeBuilder<PersistedSubscription> builder)
        {
            builder.ToTable(_tablePrefix + "Subscription", _schema);
            builder.Property(x => x.PersistenceId).UseIdentityColumn();
        }

        protected override void ConfigureWorkflowStorage(EntityTypeBuilder<PersistedWorkflow> builder)
        {
            builder.ToTable(_tablePrefix + "Workflow", _schema);
            builder.Property(x => x.PersistenceId).UseIdentityColumn();
        }
        
        protected override void ConfigureExecutionPointerStorage(EntityTypeBuilder<PersistedExecutionPointer> builder)
        {
            builder.ToTable(_tablePrefix + "ExecutionPointer", _schema);
            builder.Property(x => x.PersistenceId).UseIdentityColumn();
        }

        protected override void ConfigureExecutionErrorStorage(EntityTypeBuilder<PersistedExecutionError> builder)
        {
            builder.ToTable(_tablePrefix + "ExecutionError", _schema);
            builder.Property(x => x.PersistenceId).UseIdentityColumn();
        }

        protected override void ConfigureExetensionAttributeStorage(EntityTypeBuilder<PersistedExtensionAttribute> builder)
        {
            builder.ToTable(_tablePrefix + "ExtensionAttribute", _schema);
            builder.Property(x => x.PersistenceId).UseIdentityColumn();
        }

        protected override void ConfigureEventStorage(EntityTypeBuilder<PersistedEvent> builder)
        {
            builder.ToTable(_tablePrefix + "Event", _schema);
            builder.Property(x => x.PersistenceId).UseIdentityColumn();
        }
    }
}
