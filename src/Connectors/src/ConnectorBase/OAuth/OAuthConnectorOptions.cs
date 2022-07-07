// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Connector.OAuth;

public class OAuthConnectorOptions : AbstractServiceConnectorOptions
{
    private const string SECURITY_CLIENT_SECTION_PREFIX = "security:oauth2:client";
    private const string SECURITY_RESOURCE_SECTION_PREFIX = "security:oauth2:resource";

    public OAuthConnectorOptions()
    {
    }

    public OAuthConnectorOptions(IConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var section = config.GetSection(SECURITY_CLIENT_SECTION_PREFIX);
        section.Bind(this);
        ValidateCertificates = GetCertificateValidation(section, config, ValidateCertificates);

        section = config.GetSection(SECURITY_RESOURCE_SECTION_PREFIX);
        section.Bind(this);
    }

    public string OAuthServiceUrl { get; set; } = OAuthConnectorDefaults.Default_OAuthServiceUrl;

    public string ClientId { get; set; } = OAuthConnectorDefaults.Default_ClientId;

    public string ClientSecret { get; set; } = OAuthConnectorDefaults.Default_ClientSecret;

    public string UserAuthorizationUri { get; set; } = OAuthConnectorDefaults.Default_AuthorizationUri;

    public string AccessTokenUri { get; set; } = OAuthConnectorDefaults.Default_AccessTokenUri;

    public string UserInfoUri { get; set; } = OAuthConnectorDefaults.Default_UserInfoUri;

    public string TokenInfoUri { get; set; } = OAuthConnectorDefaults.Default_CheckTokenUri;

    public string JwtKeyUri { get; set; } = OAuthConnectorDefaults.Default_JwtTokenKey;

    public List<string> Scope { get; set; }

    public bool ValidateCertificates { get; set; } = OAuthConnectorDefaults.Default_ValidateCertificates;

    private static bool GetCertificateValidation(IConfigurationSection clientConfigsection, IConfiguration resolve, bool def)
    {
        return ConfigurationValuesHelper.GetBoolean("validate_certificates", clientConfigsection, resolve, def);
    }
}
