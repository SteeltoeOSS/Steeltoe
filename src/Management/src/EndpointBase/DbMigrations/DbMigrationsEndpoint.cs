// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Reflection;

namespace Steeltoe.Management.Endpoint.DbMigrations;

public class DbMigrationsEndpoint : AbstractEndpoint<Dictionary<string, DbMigrationsDescriptor>>, IDbMigrationsEndpoint
{
    /// <summary>
    /// Hacky class to allow mocking migration methods in unit tests.
    /// </summary>
    public class DbMigrationsEndpointHelper
    {
        internal virtual Assembly ScanRootAssembly => Assembly.GetEntryAssembly();

        internal virtual IEnumerable<string> GetPendingMigrations(object context) => GetMigrationsReflectively(context, DbMigrationsEndpoint.GetPendingMigrationsMethod);

        internal virtual IEnumerable<string> GetAppliedMigrations(object context) => GetMigrationsReflectively(context, DbMigrationsEndpoint.GetAppliedMigrationsMethod);

        internal virtual IEnumerable<string> GetMigrations(object context) => GetMigrationsReflectively(context, DbMigrationsEndpoint.GetMigrationsMethod);

        private IEnumerable<string> GetMigrationsReflectively(object dbContext, MethodInfo method)
        {
            var dbFacade = GetDatabase.Invoke(dbContext, null);
            return (IEnumerable<string>)method.Invoke(null, new[] { dbFacade });
        }
    }

    internal static readonly Type DbContextType = Type.GetType("Microsoft.EntityFrameworkCore.DbContext, Microsoft.EntityFrameworkCore");
    internal static readonly Type MigrationsExtensionsType = Type.GetType("Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions,Microsoft.EntityFrameworkCore.Relational");
    internal static readonly MethodInfo GetDatabase = DbContextType?.GetProperty("Database", BindingFlags.Public | BindingFlags.Instance).GetMethod;
    internal static readonly MethodInfo GetPendingMigrationsMethod = MigrationsExtensionsType?.GetMethod("GetPendingMigrations", BindingFlags.Static | BindingFlags.Public);
    internal static readonly MethodInfo GetAppliedMigrationsMethod = MigrationsExtensionsType?.GetMethod("GetAppliedMigrations", BindingFlags.Static | BindingFlags.Public);
    internal static readonly MethodInfo GetMigrationsMethod = MigrationsExtensionsType?.GetMethod("GetMigrations", BindingFlags.Static | BindingFlags.Public);

    private readonly IServiceProvider _container;
    private readonly DbMigrationsEndpointHelper _endpointHelper;
    private readonly ILogger<DbMigrationsEndpoint> _logger;

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
        if (DbContextType is null)
        {
            _logger?.LogCritical("DbMigrations endpoint invoked but no DbContext was found.");
        }
        else
        {
            var knownEfContexts = _endpointHelper.ScanRootAssembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .SelectMany(x => x.DefinedTypes)
                .Union(_endpointHelper.ScanRootAssembly.DefinedTypes)
                .Where(type => !type.IsAbstract && type.AsType() != DbContextType && DbContextType.GetTypeInfo().IsAssignableFrom(type.AsType()))
                .Select(typeInfo => typeInfo.AsType())
                .ToList();
            var scope = _container.CreateScope().ServiceProvider;
            foreach (var contextType in knownEfContexts)
            {
                var dbContext = scope.GetService(contextType);
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
                    _logger?.LogWarning("Encountered exception loading migrations: {exception}", e.Message);
                    descriptor.PendingMigrations = _endpointHelper.GetMigrations(dbContext).ToList();
                }
            }
        }

        return result;
    }
}
