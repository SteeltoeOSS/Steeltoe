#if NET461
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EF6
{
    public class SqlServerDbContextConnectorFactory : SqlServerProviderConnectorFactory
    {
        internal SqlServerDbContextConnectorFactory()
        {
        }

        public SqlServerDbContextConnectorFactory(SqlServerServiceInfo info, SqlServerProviderConnectorOptions config, Type dbContextType)
            : base(info, config, dbContextType)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }
        }

        public override object Create(IServiceProvider arg)
        {
            var connectionString = base.CreateConnectionString();
            object result = null;
            if (connectionString != null)
            {
                result = ConnectorHelpers.CreateInstance(_type, new object[] { connectionString });
            }

            if (result == null)
            {
                throw new ConnectorException(string.Format("Unable to create instance of '{0}', are you missing 'public {0}(string connectionString)' constructor", _type));
            }

            return result;
        }
    }
}
#endif