// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.DbMigrations;

public class DbMigrationsEndpoint : IDbMigrationsEndpoint
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
    private readonly ILogger<DbMigrationsEndpoint> _logger;

    public IEndpointOptions Options => _options.CurrentValue;

    public DbMigrationsEndpoint(IOptionsMonitor<DbMigrationsEndpointOptions> options, IServiceProvider container, ILogger<DbMigrationsEndpoint> logger)
        : this(options, container, new DbMigrationsEndpointHelper(), logger)
    {
    }

    public DbMigrationsEndpoint(IOptionsMonitor<DbMigrationsEndpointOptions> options, IServiceProvider container, DbMigrationsEndpointHelper endpointHelper,
        ILogger<DbMigrationsEndpoint> logger)
    {
        _options = options;
        _container = container;
        _endpointHelper = endpointHelper;
        _logger = logger;
    }

    public Dictionary<string, DbMigrationsDescriptor> Invoke()
    {
        return DoInvoke();
    }

    private Dictionary<string, DbMigrationsDescriptor> DoInvoke()
    {
        var result = new Dictionary<string, DbMigrationsDescriptor>();

        if (DbContextType is null)
        {
            _logger.LogCritical("DbMigrations endpoint invoked but no DbContext was found.");
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
                    descriptor.PendingMigrations = _endpointHelper.GetPendingMigrations(dbContext).ToList();
                    descriptor.AppliedMigrations = _endpointHelper.GetAppliedMigrations(dbContext).ToList();
                }
                catch (DbException e) when (e.Message.Contains("exist", StringComparison.Ordinal))
                {
                    _logger.LogWarning(e, "Encountered exception loading migrations: {exception}", e.Message);
                    descriptor.PendingMigrations = _endpointHelper.GetMigrations(dbContext).ToList();
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Hacky class to allow mocking migration methods in unit tests.
    /// </summary>
    public class DbMigrationsEndpointHelper
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
