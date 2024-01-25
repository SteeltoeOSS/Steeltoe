// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Eureka;

internal sealed class ThisServiceInstance : IServiceInstance
{
    private readonly IOptionsMonitor<EurekaInstanceOptions> _optionsMonitor;

    private EurekaInstanceOptions Options => _optionsMonitor.CurrentValue;

    public string ServiceId => Options.AppName;
    public string Host => Options.ResolveHostName(false);
    public int Port => GetPort();
    public bool IsSecure => Options.SecurePortEnabled;
    public Uri Uri => GetUri();
    public IDictionary<string, string> Metadata => Options.MetadataMap;

    public ThisServiceInstance(IOptionsMonitor<EurekaInstanceOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    private Uri GetUri()
    {
        string scheme = IsSecure ? "https" : "http";
        return new Uri($"{scheme}://{Host}:{Port}");
    }

    private int GetPort()
    {
        if (IsSecure)
        {
            return Options.SecurePort == -1 ? EurekaInstanceConfiguration.DefaultSecurePort : Options.SecurePort;
        }

        return Options.NonSecurePort == -1 ? EurekaInstanceConfiguration.DefaultNonSecurePort : Options.NonSecurePort;
    }
}
