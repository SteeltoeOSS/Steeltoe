// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Connector
{
    internal class ConnectionStringConfigurationProvider : ConfigurationProvider
    {
        public IConfiguration Configuration { get; private set; }

        internal IList<IConfigurationProvider> _providers;

        internal ConnectionStringManager ConnectionStringManager { get; set; }

        internal ServiceInfoCreator ServiceInfoCreator { get; set; }

        public ConnectionStringConfigurationProvider(IList<IConfigurationProvider> providers)
        {
            _providers = providers;
        }

        public IList<IConfigurationProvider> Providers => _providers;

        public new IChangeToken GetReloadToken()
        {
            EnsureInitialized();
            return Configuration.GetReloadToken();
        }

        public override void Load()
        {
            Configuration ??= new ConfigurationRoot(_providers);
            ConnectionStringManager ??= new ConnectionStringManager(Configuration);
        }

        public override bool TryGet(string key, out string value)
        {
            if (key.StartsWith("ConnectionStrings"))
            {
                var searchKey = key.Split(':')[1];

                try
                {
                    // look for a service info of that type
                    value = ConnectionStringManager.GetByTypeName(searchKey).ConnectionString;
                    return true;
                }
                catch (ConnectorException)
                {
                    // look for a service info with that id
                    ServiceInfoCreator ??= IConfigurationExtensions.GetServiceInfoCreator(Configuration);
                    var serviceInfo = ServiceInfoCreator.ServiceInfos.FirstOrDefault(si => si.Id.Equals(searchKey));
                    if (serviceInfo is object)
                    {
                        value = ConnectionStringManager.GetFromServiceInfo(serviceInfo).ConnectionString;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private void EnsureInitialized()
        {
            if (Configuration == null)
            {
                Configuration = new ConfigurationRoot(_providers);
            }
        }
    }
}