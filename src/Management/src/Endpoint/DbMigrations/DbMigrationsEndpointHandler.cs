// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.DbMigrations;

internal sealed class DbMigrationsEndpointHandler : IDbMigrationsEndpointHandler
{
    private readonly IOptionsMonitor<DbMigrationsEndpointOptions> _optionsMonitor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDatabaseMigrationScanner _scanner;
    private readonly ILogger<DbMigrationsEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public DbMigrationsEndpointHandler(IOptionsMonitor<DbMigrationsEndpointOptions> optionsMonitor, IServiceProvider serviceProvider,
        IDatabaseMigrationScanner scanner, ILoggerFactory loggerFactory)
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

    public async Task<Dictionary<string, DbMigrationsDescriptor>> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, DbMigrationsDescriptor>();
        Type? dbContextType = DatabaseMigrationScanner.DbContextType;

        if (dbContextType is null)
        {
            _logger.LogCritical("DbMigrations endpoint was invoked but no DbContext was found.");
        }
        else
        {
            // @formatter:wrap_chained_method_calls chop_always
            // @formatter:wrap_after_property_in_chained_method_calls true

            List<Type> knownDbContextTypes = _scanner.AssemblyToScan
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .SelectMany(assembly => assembly.DefinedTypes)
                .Union(_scanner.AssemblyToScan.DefinedTypes)
                .Where(type => !type.IsAbstract && type.AsType() != dbContextType && dbContextType.GetTypeInfo()
                    .IsAssignableFrom(type.AsType()))
                .Select(typeInfo => typeInfo.AsType())
                .ToList();

            // @formatter:wrap_after_property_in_chained_method_calls restore
            // @formatter:wrap_chained_method_calls restore

            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();

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
                    AddRange(descriptor.PendingMigrations, _scanner.GetPendingMigrations(dbContext));
                    AddRange(descriptor.AppliedMigrations, _scanner.GetAppliedMigrations(dbContext));
                }
                catch (DbException exception) when (exception.Message.Contains("exist", StringComparison.Ordinal))
                {
                    _logger.LogWarning(exception, "Encountered exception loading migrations: {exception}", exception.Message);
                    AddRange(descriptor.PendingMigrations, _scanner.GetMigrations(dbContext));
                }
            }
        }

        return result;
    }

    private static void AddRange<T>(IList<T> source, IEnumerable<T> items)
    {
        ArgumentGuard.NotNull(source);
        ArgumentGuard.NotNull(items);

        if (source is List<T> list)
        {
            list.AddRange(items);
        }
        else
        {
            foreach (T item in items)
            {
                source.Add(item);
            }
        }
    }
}
