// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Discovery.Eureka;

public class EurekaApplicationInfoManager : ApplicationInfoManager
{
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instConfig;

    public override EurekaInstanceConfiguration InstanceConfig => _instConfig.CurrentValue;

    public EurekaApplicationInfoManager(IOptionsMonitor<EurekaInstanceOptions> instConfig, ILoggerFactory loggerFactory = null)
    {
        _instConfig = instConfig;
        Initialize(InstanceConfig, loggerFactory);
    }
}
