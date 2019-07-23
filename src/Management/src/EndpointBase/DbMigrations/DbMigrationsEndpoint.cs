// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Management.EndpointBase.DbMigrations
{
    public class DbMigrationsEndpoint : AbstractEndpoint<Dictionary<string, DbMigrationsDescriptor>>
    {
        /// <summary>
        /// Hacky class to allow mocking migration methods in unit tests
        /// </summary>
        public class DbMigrationsEndpointHelper
        {
            internal virtual Assembly ScanRootAssembly => Assembly.GetEntryAssembly();

            internal virtual IEnumerable<string> GetPendingMigrations(object context) => GetMigrationsReflectively(context, _getPendingMigrations);

            internal virtual IEnumerable<string> GetAppliedMigrations(object context) => GetMigrationsReflectively(context, _getAppliedMigrations);

            internal virtual IEnumerable<string> GetMigrations(object context) => GetMigrationsReflectively(context, _getMigrations);

            private IEnumerable<string> GetMigrationsReflectively(object dbContext, MethodInfo method)
            {
                var dbFacade = _getDatabase.Invoke(dbContext, null);
                return (IEnumerable<string>)method.Invoke(null, new[] { dbFacade });
            }
        }

        internal static Type _dbContextType;
        internal static MethodInfo _getDatabase;
        internal static MethodInfo _getPendingMigrations;
        internal static MethodInfo _getAppliedMigrations;
        internal static MethodInfo _getMigrations;

        private readonly IServiceProvider _container;
        private readonly DbMigrationsEndpointHelper _endpointHelper;
        private readonly ILogger<DbMigrationsEndpoint> _logger;

        static DbMigrationsEndpoint()
        {
            _dbContextType = Type.GetType("Microsoft.EntityFrameworkCore.DbContext, Microsoft.EntityFrameworkCore");
            _getDatabase = _dbContextType.GetProperty("Database", BindingFlags.Public | BindingFlags.Instance).GetMethod;
            var extensions = Type.GetType("Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions,Microsoft.EntityFrameworkCore.Relational");
            _getPendingMigrations = extensions.GetMethod("GetPendingMigrations", BindingFlags.Static | BindingFlags.Public);
            _getAppliedMigrations = extensions.GetMethod("GetAppliedMigrations", BindingFlags.Static | BindingFlags.Public);
            _getMigrations = extensions.GetMethod("GetMigrations", BindingFlags.Static | BindingFlags.Public);
        }

        public DbMigrationsEndpoint(
            IDbMigrationsOptions options,
            IServiceProvider container,
            ILogger<DbMigrationsEndpoint> logger = null)
            : this(options, container, new DbMigrationsEndpointHelper(), logger)
        {
        }

        public DbMigrationsEndpoint(
            IDbMigrationsOptions options,
            IServiceProvider container,
            DbMigrationsEndpointHelper endpointHelper,
            ILogger<DbMigrationsEndpoint> logger = null)
            : base(options)
        {
            _container = container;
            _endpointHelper = endpointHelper;
            _logger = logger;
        }

        public override Dictionary<string, DbMigrationsDescriptor> Invoke() => DoInvoke();

        private Dictionary<string, DbMigrationsDescriptor> DoInvoke()
        {
            var result = new Dictionary<string, DbMigrationsDescriptor>();
            var knownEfContexts = _endpointHelper.ScanRootAssembly
                    .GetReferencedAssemblies()
                    .Select(Assembly.Load)
                    .SelectMany(x => x.DefinedTypes)
                    .Union(_endpointHelper.ScanRootAssembly.DefinedTypes)
                    .Where(type => !type.IsAbstract && type.AsType() != _dbContextType && _dbContextType.GetTypeInfo().IsAssignableFrom(type.AsType()))
                    .Select(typeInfo => typeInfo.AsType())
                    .ToList();
            foreach (var contextType in knownEfContexts)
            {
                var dbContext = _container.GetService(contextType);
                if (dbContext == null)
                {
                    continue;
                }

                var descriptor = new DbMigrationsDescriptor();
                var contextName = dbContext.GetType().Name;
                result.Add(contextName, descriptor);
                try
                {
                    descriptor.PendingMigrations = _endpointHelper.GetPendingMigrations(dbContext).ToList();
                    descriptor.AppliedMigrations = _endpointHelper.GetAppliedMigrations(dbContext).ToList();
                }
                catch (DbException e) when (e.Message.Contains("exist"))
                {
                    // todo: maybe improve detection logic when database is new. hard to do generically across all providers
                    descriptor.PendingMigrations = _endpointHelper.GetMigrations(dbContext).ToList();
                }
            }

            return result;
        }
    }
}