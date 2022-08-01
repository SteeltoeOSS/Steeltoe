// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Configured CredHub client.
/// </summary>
public class CredHubOptions
{
    /// <summary>
    /// Gets or sets routable address of CredHub server.
    /// </summary>
    public string CredHubUrl { get; set; } = "https://credhub.service.cf.internal:8844/api";

    /// <summary>
    /// Gets or sets Client Id for interactions with UAA.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets Client Secret for interactions with UAA.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validate server certificates for UAA and CredHub servers.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Perform basic validation to make sure a Client Id and Secret have been provided.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(ClientId))
        {
            throw new ArgumentException("A Client Id is required for the CredHub Client");
        }

        if (string.IsNullOrEmpty(ClientSecret))
        {
            throw new ArgumentException("A Client Secret is required for the CredHub Client");
        }
    }
}
