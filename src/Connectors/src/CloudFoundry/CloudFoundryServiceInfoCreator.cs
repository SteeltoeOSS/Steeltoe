// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.Connector.CloudFoundry;

public class CloudFoundryServiceInfoCreator : ServiceInfoCreator
{
    private static readonly object Lock = new();
    private static CloudFoundryServiceInfoCreator _me;

    public new static bool IsRelevant => Platform.IsCloudFoundry;

    private CloudFoundryServiceInfoCreator(IConfiguration configuration)
        : base(configuration)
    {
        BuildServiceInfoFactories();
        BuildServiceInfos();
    }

    public new static CloudFoundryServiceInfoCreator Instance(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        if (configuration != _me?.Configuration)
        {
            lock (Lock)
            {
                if (configuration != _me?.Configuration)
                {
                    _me = new CloudFoundryServiceInfoCreator(configuration);
                }
            }
        }

        return _me;
    }

    private void BuildServiceInfos()
    {
        ServiceInfos.Clear();

        var appInfo = new CloudFoundryApplicationOptions(Configuration);
        var serviceOptions = new CloudFoundryServicesOptions(Configuration);

        foreach (KeyValuePair<string, IEnumerable<Service>> serviceOption in serviceOptions.Services)
        {
            foreach (Service s in serviceOption.Value)
            {
                IServiceInfoFactory factory = FindFactory(s);

                if (factory != null && factory.Create(s) is ServiceInfo info)
                {
                    info.ApplicationInfo = appInfo;
                    ServiceInfos.Add(info);
                }
            }
        }
    }
}
