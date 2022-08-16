// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class HystrixRabbitMQServiceInfo : ServiceInfo
{
    public RabbitMQServiceInfo RabbitInfo { get; }

    public string Scheme => RabbitInfo.Scheme;

    public string Query => RabbitInfo.Query;

    public string Path => RabbitInfo.Path;

    public string Uri => RabbitInfo.Uri;

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    public List<string> Uris => RabbitInfo.Uris;
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs

    public string Host => RabbitInfo.Host;

    public int Port => RabbitInfo.Port;

    public string UserName => RabbitInfo.UserName;

    public string Password => RabbitInfo.Password;

    public bool IsSslEnabled { get; }

    public HystrixRabbitMQServiceInfo(string id, string uri, bool sslEnabled)
        : base(id)
    {
        IsSslEnabled = sslEnabled;
        RabbitInfo = new RabbitMQServiceInfo(id, uri);
    }

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    public HystrixRabbitMQServiceInfo(string id, string uri, List<string> uris, bool sslEnabled)
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs
        : base(id)
    {
        RabbitInfo = new RabbitMQServiceInfo(id, uri, null, uris, null);
        IsSslEnabled = sslEnabled;
    }
}
