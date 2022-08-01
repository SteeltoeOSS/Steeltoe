// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Connector;

internal sealed class ConnectionStringConfigurationProvider : ConfigurationProvider
{
    internal Lazy<IConfiguration> Configuration { get; private set; }

    internal IList<IConfigurationProvider> Providers;

    internal Lazy<ConnectionStringManager> ConnectionStringManager { get; set; }

    internal ServiceInfoCreator ServiceInfoCreator { get; set; }

    public ConnectionStringConfigurationProvider(IEnumerable<IConfigurationProvider> providers)
    {
        if (providers is null)
        {
            throw new ArgumentNullException(nameof(providers));
        }

        this.Providers = providers.ToList();
        Configuration = new Lazy<IConfiguration>(() => new ConfigurationRoot(this.Providers));
        ConnectionStringManager = new Lazy<ConnectionStringManager>(() => new ConnectionStringManager(Configuration.Value));
    }

    public new IChangeToken GetReloadToken() => Configuration.Value.GetReloadToken();

    /// <inheritdoc />
    public override bool TryGet(string key, out string value)
    {
        if (key.StartsWith("ConnectionStrings:", StringComparison.InvariantCultureIgnoreCase))
        {
            var searchKey = key.Split(':')[1];

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
                var serviceInfo = ServiceInfoCreator.ServiceInfos.FirstOrDefault(si => si.Id.Equals(searchKey));
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
