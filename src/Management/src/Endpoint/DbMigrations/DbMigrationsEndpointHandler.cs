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
    private static readonly Type? MigrationsExtensionsType =
        Type.GetType("Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions,Microsoft.EntityFrameworkCore.Relational");

    internal static readonly Type? DbContextType = Type.GetType("Microsoft.EntityFrameworkCore.DbContext, Microsoft.EntityFrameworkCore");

    internal static readonly MethodInfo? GetDatabaseMethod = DbContextType?.GetProperty("Database", BindingFlags.Public | BindingFlags.Instance)?.GetMethod;

    internal static readonly MethodInfo? GetPendingMigrationsMethod =
        MigrationsExtensionsType?.GetMethod("GetPendingMigrations", BindingFlags.Static | BindingFlags.Public);

    internal static readonly MethodInfo? GetAppliedMigrationsMethod =
        MigrationsExtensionsType?.GetMethod("GetAppliedMigrations", BindingFlags.Static | BindingFlags.Public);

    internal static readonly MethodInfo? GetMigrationsMethod = MigrationsExtensionsType?.GetMethod("GetMigrations", BindingFlags.Static | BindingFlags.Public);

    private readonly IOptionsMonitor<DbMigrationsEndpointOptions> _optionsMonitor;
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseMigrationScanner _scanner;
    private readonly ILogger<DbMigrationsEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public DbMigrationsEndpointHandler(IOptionsMonitor<DbMigrationsEndpointOptions> optionsMonitor, IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
        : this(optionsMonitor, serviceProvider, new DatabaseMigrationScanner(), loggerFactory)
    {
    }

    public DbMigrationsEndpointHandler(IOptionsMonitor<DbMigrationsEndpointOptions> optionsMonitor, IServiceProvider serviceProvider,
        DatabaseMigrationScanner scanner, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(scanner);
        ArgumentGuard.NotNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _serviceProvider = serviceProvider;
        _scanner = scanner;
        _logger = loggerFactory.CreateLogger<DbMigrationsEndpointHandler>();
    }

    public Task<Dictionary<string, DbMigrationsDescriptor>> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, DbMigrationsDescriptor>();

        if (DbContextType is null)
        {
            _logger.LogCritical("DbMigrations endpoint was invoked but no DbContext was found.");
        }
        else
        {
            // @formatter:wrap_chained_method_calls chop_always
            // @formatter:keep_existing_linebreaks true

            List<Type> knownDbContextTypes = _scanner.ScanRootAssembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .SelectMany(assembly => assembly.DefinedTypes)
                .Union(_scanner.ScanRootAssembly.DefinedTypes)
                .Where(type => !type.IsAbstract && type.AsType() != DbContextType && DbContextType.GetTypeInfo()
                    .IsAssignableFrom(type.AsType()))
                .Select(typeInfo => typeInfo.AsType())
                .ToList();

            // @formatter:keep_existing_linebreaks restore
            // @formatter:wrap_chained_method_calls restore

            using IServiceScope scope = _serviceProvider.CreateScope();

            foreach (Type contextType in knownDbContextTypes)
            {
                object? dbContext = scope.ServiceProvider.GetService(contextType);

                if (dbContext == null)
                {
                    continue;
                }

                var descriptor = new DbMigrationsDescriptor();
                string contextName = dbContext.GetType().Name;
                result.Add(contextName, descriptor);

                try
                {
                    descriptor.PendingMigrations.AddRange(_scanner.GetPendingMigrations(dbContext));
                    descriptor.AppliedMigrations.AddRange(_scanner.GetAppliedMigrations(dbContext));
                }
                catch (DbException exception) when (exception.Message.Contains("exist", StringComparison.Ordinal))
                {
                    _logger.LogWarning(exception, "Encountered exception loading migrations: {exception}", exception.Message);
                    descriptor.PendingMigrations.AddRange(_scanner.GetMigrations(dbContext));
                }
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Hacky class to allow mocking migration methods in unit tests.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    internal class DatabaseMigrationScanner
    {
        internal virtual Assembly ScanRootAssembly => Assembly.GetEntryAssembly()!;

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

        private IEnumerable<string> GetMigrationsReflectively(object dbContext, MethodInfo? method)
        {
            if (GetDatabaseMethod == null || method == null)
            {
                return Array.Empty<string>();
            }

            object? dbFacade = GetDatabaseMethod.Invoke(dbContext, null);

            return (IEnumerable<string>)method.Invoke(null, new[]
            {
                dbFacade
            })!;
        }
    }
}
