// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class VcapServicesConfigServerCredentialsOptions
{
    public string? Uri { get; set; }

    [ConfigurationKeyName("Client_Id")]
    public string? ClientId { get; set; }

    [ConfigurationKeyName("Client_Secret")]
    public string? ClientSecret { get; set; }

    [ConfigurationKeyName("Access_Token_Uri")]
    public string? AccessTokenUri { get; set; }
}
