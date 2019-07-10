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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Management.EndpointBase.DbMigrations
{
    public class EntityFrameworkEndpoint : AbstractEndpoint<Dictionary<string, EntityFrameworkDescriptor>>
    {
        /// <summary>
        /// Hacky class to allow mocking migration methods in unit tests
        /// </summary>
        public class EntityFrameworkEndpointHelper
        {
            internal virtual Assembly ScanRootAssembly => Assembly.GetEntryAssembly();

            internal virtual IEnumerable<string> GetPendingMigrations(DbContext context) => context.Database.GetPendingMigrations();

            internal virtual IEnumerable<string> GetAppliedMigrations(DbContext context) => context.Database.GetAppliedMigrations();

            internal virtual IEnumerable<string> GetMigrations(DbContext context) => context.Database.GetMigrations();
        }

        private readonly IServiceProvider _container;
        private readonly EntityFrameworkEndpointHelper _endpointHelper;
        private readonly ILogger<EntityFrameworkEndpoint> _logger;

        public EntityFrameworkEndpoint(
            IEntityFrameworkOptions options,
            IServiceProvider container,
            ILogger<EntityFrameworkEndpoint> logger = null)
            : this(options, container, new EntityFrameworkEndpointHelper(), logger)
        {
        }

        public EntityFrameworkEndpoint(
            IEntityFrameworkOptions options,
            IServiceProvider container,
            EntityFrameworkEndpointHelper endpointHelper,
            ILogger<EntityFrameworkEndpoint> logger = null)
            : base(options)
        {
            _container = container;
            _endpointHelper = endpointHelper;
            _logger = logger;
            if (options.AutoDiscoverContexts)
            {
                options.ContextTypes = endpointHelper.ScanRootAssembly
                    .GetReferencedAssemblies()
                    .Select(Assembly.Load)
                    .SelectMany(x => x.DefinedTypes)
                    .Union(endpointHelper.ScanRootAssembly.DefinedTypes)
                    .Where(type => !type.IsAbstract && type.AsType() != typeof(DbContext) && typeof(DbContext).GetTypeInfo().IsAssignableFrom(type.AsType()))
                    .Select(typeInfo => typeInfo.AsType())
                    .ToList();
            }
        }

        public new IEntityFrameworkOptions Options => options as IEntityFrameworkOptions;

        public override Dictionary<string, EntityFrameworkDescriptor> Invoke() => DoInvoke();

        private Dictionary<string, EntityFrameworkDescriptor> DoInvoke()
        {
            var result = new Dictionary<string, EntityFrameworkDescriptor>();
            foreach (var contextType in Options.ContextTypes)
            {
                var dbContext = (DbContext)_container.GetService(contextType);
                if (dbContext == null)
                {
                    _logger.LogWarning($"{contextType.FullName} is not registered in DI container");
                    continue;
                }

                var descriptor = new EntityFrameworkDescriptor();
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