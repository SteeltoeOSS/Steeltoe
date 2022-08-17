// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Hystrix;

public class HystrixProviderConnectorOptions : AbstractServiceConnectorOptions
{
    private const string HystrixClientSectionPrefix = "hystrix:client";
    public const string DefaultScheme = "amqp";
    public const string DefaultSslScheme = "amqps";
    public const string DefaultServer = "127.0.0.1";
    public const int DefaultPort = 5672;
    public const int DefaultSslPort = 5671;

    public bool SslEnabled { get; set; }

    public string Uri { get; set; }

    public string Server { get; set; } = DefaultServer;

    public int Port { get; set; } = DefaultPort;

    public int SslPort { get; set; } = DefaultSslPort;

    public string Username { get; set; }

    public string Password { get; set; }

    public string VirtualHost { get; set; }

    public HystrixProviderConnectorOptions()
    {
    }

    public HystrixProviderConnectorOptions(IConfiguration config)
    {
        ArgumentGuard.NotNull(config);

        IConfigurationSection section = config.GetSection(HystrixClientSectionPrefix);
        section.Bind(this);
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Uri))
        {
            return Uri;
        }

        UriInfo uri = SslEnabled
            ? new UriInfo(DefaultSslScheme, Server, SslPort, Username, Password, VirtualHost)
            : new UriInfo(DefaultScheme, Server, Port, Username, Password, VirtualHost);

        return uri.ToString();
    }
}
