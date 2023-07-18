// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Util;

namespace Steeltoe.Management.Endpoint.DbMigrations;

internal sealed class DbMigrationsEndpointHandler : IDbMigrationsEndpointHandler
{
    internal static readonly Type DbContextType = Type.GetType("Microsoft.EntityFrameworkCore.DbContext, Microsoft.EntityFrameworkCore");

    internal static readonly Type MigrationsExtensionsType =
        Type.GetType("Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions,Microsoft.EntityFrameworkCore.Relational");

    internal static readonly MethodInfo GetDatabase = DbContextType?.GetProperty("Database", BindingFlags.Public | BindingFlags.Instance).GetMethod;

    internal static readonly MethodInfo GetPendingMigrationsMethod =
        MigrationsExtensionsType?.GetMethod("GetPendingMigrations", BindingFlags.Static | BindingFlags.Public);

    internal static readonly MethodInfo GetAppliedMigrationsMethod =
        MigrationsExtensionsType?.GetMethod("GetAppliedMigrations", BindingFlags.Static | BindingFlags.Public);

    internal static readonly MethodInfo GetMigrationsMethod = MigrationsExtensionsType?.GetMethod("GetMigrations", BindingFlags.Static | BindingFlags.Public);
    private readonly IOptionsMonitor<DbMigrationsEndpointOptions> _options;
    private readonly IServiceProvider _container;
    private readonly DbMigrationsEndpointHelper _endpointHelper;
    private readonly ILogger<DbMigrationsEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public DbMigrationsEndpointHandler(IOptionsMonitor<DbMigrationsEndpointOptions> options, IServiceProvider container, ILoggerFactory loggerFactory)
        : this(options, container, new DbMigrationsEndpointHelper(), loggerFactory)
    {
    }

    public DbMigrationsEndpointHandler(IOptionsMonitor<DbMigrationsEndpointOptions> options, IServiceProvider container,
        DbMigrationsEndpointHelper endpointHelper, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(container);
        ArgumentGuard.NotNull(endpointHelper);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _container = container;
        _endpointHelper = endpointHelper;
        _logger = loggerFactory.CreateLogger<DbMigrationsEndpointHandler>();
    }

    private Dictionary<string, DbMigrationsDescriptor> DoInvoke()
    {
        var result = new Dictionary<string, DbMigrationsDescriptor>();

        if (DbContextType is null)
        {
            _logger.LogCritical("DbMigrations endpoint was invoked but no DbContext was found.");
        }
        else
        {
            List<Type> knownEfContexts = _endpointHelper.ScanRootAssembly.GetReferencedAssemblies().Select(Assembly.Load).SelectMany(x => x.DefinedTypes)
                .Union(_endpointHelper.ScanRootAssembly.DefinedTypes)
                .Where(type => !type.IsAbstract && type.AsType() != DbContextType && DbContextType.GetTypeInfo().IsAssignableFrom(type.AsType()))
                .Select(typeInfo => typeInfo.AsType()).ToList();

            IServiceProvider scope = _container.CreateScope().ServiceProvider;

            foreach (Type contextType in knownEfContexts)
            {
                object dbContext = scope.GetService(contextType);

                if (dbContext == null)
                {
                    continue;
                }

                var descriptor = new DbMigrationsDescriptor();
                string contextName = dbContext.GetType().Name;
                result.Add(contextName, descriptor);

                try
                {
                    descriptor.PendingMigrations.AddRange(_endpointHelper.GetPendingMigrations(dbContext));
                    descriptor.AppliedMigrations.AddRange(_endpointHelper.GetAppliedMigrations(dbContext));
                }
                catch (DbException e) when (e.Message.Contains("exist", StringComparison.Ordinal))
                {
                    _logger.LogWarning(e, "Encountered exception loading migrations: {exception}", e.Message);
                    descriptor.PendingMigrations.AddRange(_endpointHelper.GetMigrations(dbContext));
                }
            }
        }

        return result;
    }

    public Task<Dictionary<string, DbMigrationsDescriptor>> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        return Task.FromResult(DoInvoke());
    }

    /// <summary>
    /// Hacky class to allow mocking migration methods in unit tests.
    /// </summary>
    internal class DbMigrationsEndpointHelper
    {
        internal virtual Assembly ScanRootAssembly => Assembly.GetEntryAssembly();

        internal virtual IEnumerable<string> GetPendingMigrations(object context)
        {
            return GetMigrationsReflectively(context, GetPendingMigrationsMethod);
        }

        internal virtual IEnumerable<string> GetAppliedMigrations(object context)
        {
            return GetMigrationsReflectively(context, GetAppliedMigrationsMethod);
        }

        internal virtual IEnumerable<string> GetMigrations(object context)
        {
            return GetMigrationsReflectively(context, GetMigrationsMethod);
        }

        private IEnumerable<string> GetMigrationsReflectively(object dbContext, MethodInfo method)
        {
            object dbFacade = GetDatabase.Invoke(dbContext, null);

            return (IEnumerable<string>)method.Invoke(null, new[]
            {
                dbFacade
            });
        }
    }
}
