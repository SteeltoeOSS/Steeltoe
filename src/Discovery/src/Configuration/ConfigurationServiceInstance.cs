// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Configuration;

// Justification: Nullability doesn't match IServiceInstance because this type is loaded from configuration. [Required] usage should protect against nulls.
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

/// <summary>
/// Represents an <see cref="IServiceInstance" /> inside <see cref="ConfigurationDiscoveryOptions" /> that was loaded from app configuration.
/// </summary>
public sealed class ConfigurationServiceInstance : IServiceInstance
{
    /// <inheritdoc />
    IReadOnlyDictionary<string, string?> IServiceInstance.Metadata => Metadata.AsReadOnly();

    /// <inheritdoc />
    [Required]
    public string? ServiceId { get; set; }

    /// <inheritdoc />
    [Required]
    public string? Host { get; set; }

    /// <inheritdoc />
    public int Port { get; set; }

    /// <inheritdoc />
    public bool IsSecure { get; set; }

    /// <inheritdoc />
    public Uri Uri => new($"{(IsSecure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp)}{Uri.SchemeDelimiter}{Host}:{Port}");

    /// <inheritdoc cref="IServiceInstance.Metadata" />
    public IDictionary<string, string?> Metadata { get; } = new Dictionary<string, string?>();
}
