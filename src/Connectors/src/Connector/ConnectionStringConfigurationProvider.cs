// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector;

internal sealed class ConnectionStringConfigurationProvider : ConfigurationProvider
{
    internal IList<IConfigurationProvider> Providers;

    internal Lazy<IConfiguration> Configuration { get; }

    internal Lazy<ConnectionStringManager> ConnectionStringManager { get; set; }

    internal ServiceInfoCreator ServiceInfoCreator { get; set; }

    public ConnectionStringConfigurationProvider(IEnumerable<IConfigurationProvider> providers)
    {
        ArgumentGuard.NotNull(providers);

        Providers = providers.ToList();
        Configuration = new Lazy<IConfiguration>(() => new ConfigurationRoot(Providers));
        ConnectionStringManager = new Lazy<ConnectionStringManager>(() => new ConnectionStringManager(Configuration.Value));
    }

    public new IChangeToken GetReloadToken()
    {
        return Configuration.Value.GetReloadToken();
    }

    /// <inheritdoc />
    public override bool TryGet(string key, out string value)
    {
        if (key.StartsWith("ConnectionStrings:", StringComparison.InvariantCultureIgnoreCase))
        {
            string searchKey = key.Split(':')[1];

            try
            {
                // look for a service info of that type
                value = ConnectionStringManager.Value.GetByTypeName(searchKey).ConnectionString;
                return true;
            }
            catch (ConnectorException)
            {
                // look for a service info with that id
                ServiceInfoCreator = ServiceInfoCreatorFactory.GetServiceInfoCreator(Configuration.Value);
                IServiceInfo serviceInfo = ServiceInfoCreator.ServiceInfos.FirstOrDefault(si => si.Id.Equals(searchKey));

                if (serviceInfo != null)
                {
                    value = ConnectionStringManager.Value.GetFromServiceInfo(serviceInfo).ConnectionString;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }
}
