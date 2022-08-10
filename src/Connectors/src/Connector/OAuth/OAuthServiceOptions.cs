// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.OAuth;

public class OAuthServiceOptions
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string UserAuthorizationUrl { get; set; }

    public string AccessTokenUrl { get; set; }

    public string UserInfoUrl { get; set; }

    public string TokenInfoUrl { get; set; }

    public string JwtKeyUrl { get; set; }

    public ICollection<string> Scope { get; set; } = new HashSet<string>();

    public bool ValidateCertificates { get; set; } = true;
}
