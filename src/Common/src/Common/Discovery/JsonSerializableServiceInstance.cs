// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Steeltoe.Common.Discovery;

public class JsonSerializableServiceInstance : IServiceInstance
{
    public string ServiceId { get; set; }

    public string Host { get; set; }

    public int Port { get; set; }

    public bool IsSecure { get; set; }

    public Uri Uri { get; set; }

    public IDictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializableServiceInstance" /> class. For use with <see cref="JsonSerializer" />.
    /// </summary>
    public JsonSerializableServiceInstance()
    {
    }

    public JsonSerializableServiceInstance(IServiceInstance instance)
    {
        ServiceId = instance.ServiceId;
        Host = instance.Host;
        Port = instance.Port;
        IsSecure = instance.IsSecure;
        Uri = instance.Uri;
        Metadata = instance.Metadata;
    }
}
