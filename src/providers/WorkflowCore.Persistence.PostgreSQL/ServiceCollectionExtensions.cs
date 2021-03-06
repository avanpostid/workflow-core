﻿using System;
using System.Linq;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UsePostgreSQL(this WorkflowOptions options, string connectionString,
            bool canCreateDB, bool canMigrateDB, string schema = null)
        {
            var mi = new MigrationMetaInfo(schema ?? "public");
            options.UsePersistence(sp =>
                new EntityFrameworkPersistenceProvider(new PostgresContextFactory(connectionString), canCreateDB,
                    canMigrateDB));
            return options;
        }
    }
}
