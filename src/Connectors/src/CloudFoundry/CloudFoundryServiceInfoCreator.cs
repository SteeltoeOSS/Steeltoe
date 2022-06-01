// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.Connector.CloudFoundry;

public class CloudFoundryServiceInfoCreator : ServiceInfoCreator
{
    private static readonly object _lock = new ();
    private static CloudFoundryServiceInfoCreator _me;

    private CloudFoundryServiceInfoCreator(IConfiguration configuration)
        : base(configuration)
    {
        BuildServiceInfoFactories();
        BuildServiceInfos();
    }

    public static new CloudFoundryServiceInfoCreator Instance(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (configuration != _me?.Configuration)
        {
            lock (_lock)
            {
                if (configuration != _me?.Configuration)
                {
                    _me = new CloudFoundryServiceInfoCreator(configuration);
                }
            }
        }

        return _me;
    }

    public static new bool IsRelevant => Platform.IsCloudFoundry;

    private void BuildServiceInfos()
    {
        ServiceInfos.Clear();

        var appInfo = new CloudFoundryApplicationOptions(Configuration);
        var serviceOpts = new CloudFoundryServicesOptions(Configuration);

        foreach (var serviceopt in serviceOpts.Services)
        {
            foreach (var s in serviceopt.Value)
            {
                var factory = FindFactory(s);
                if (factory != null && factory.Create(s) is ServiceInfo info)
                {
                    info.ApplicationInfo = appInfo;
                    ServiceInfos.Add(info);
                }
            }
        }
    }
}
