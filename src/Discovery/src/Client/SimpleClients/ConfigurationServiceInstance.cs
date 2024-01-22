// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Client.SimpleClients;

/// <summary>
/// Represents an <see cref="IServiceInstance" /> inside <see cref="ConfigurationDiscoveryOptions" /> that was loaded from app configuration.
/// </summary>
public sealed class ConfigurationServiceInstance : IServiceInstance
{
    [Required]
    public string? ServiceId { get; set; }

    [Required]
    public string? Host { get; set; }

    public int Port { get; set; }

    public bool IsSecure { get; set; }

    public Uri Uri => new($"{(IsSecure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp)}{Uri.SchemeDelimiter}{Host}:{Port}");

    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
}
