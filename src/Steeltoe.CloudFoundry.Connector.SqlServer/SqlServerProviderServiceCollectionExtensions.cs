using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public static class SqlServerProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            SqlServerServiceInfo info = config.GetSingletonServiceInfo<SqlServerServiceInfo>();
            DoAdd(services, info, config, contextLifetime);

            return services;
        }

        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            SqlServerServiceInfo info = config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
            DoAdd(services, info, config, contextLifetime);

            return services;
        }

        private static string[] SqlServerAssemblies = new string[] { "System.Data.SqlClient" };

        private static string[] SqlServerTypeNames = new string[] { "System.Data.SqlClient.SqlConnection" };

        private static void DoAdd(IServiceCollection services, SqlServerServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type SqlServerConnection = ConnectorHelpers.FindType(SqlServerAssemblies, SqlServerTypeNames);
            if (SqlServerConnection == null)
            {
                throw new ConnectorException("Unable to find SqlServerConnection, are you missing SqlServer ADO.NET assembly");
            }

            SqlServerProviderConnectorOptions SqlServerConfig = new SqlServerProviderConnectorOptions(config);
            SqlServerProviderConnectorFactory factory = new SqlServerProviderConnectorFactory(info, SqlServerConfig, SqlServerConnection);
            services.Add(new ServiceDescriptor(SqlServerConnection, factory.Create, contextLifetime));
        }
    }
}
