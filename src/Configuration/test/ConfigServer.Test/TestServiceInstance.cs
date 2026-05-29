// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal sealed class TestServiceInstance(string serviceId, string instanceId, Uri uri, IReadOnlyDictionary<string, string?> metadata) : IServiceInstance
{
    public string ServiceId { get; } = serviceId;
    public string InstanceId { get; } = instanceId;
    public string Host { get; } = uri.Host;
    public int Port { get; } = uri.Port;
    public bool IsSecure { get; } = uri.Scheme == Uri.UriSchemeHttps;
    public Uri Uri { get; } = uri;
    public Uri? NonSecureUri => IsSecure ? null : Uri;
    public Uri? SecureUri => IsSecure ? Uri : null;
    public IReadOnlyDictionary<string, string?> Metadata { get; } = metadata;
}
