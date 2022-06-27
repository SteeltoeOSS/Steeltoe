// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public class SpringBootAdminClientOptions
{
    private const string PREFIX = "spring:boot:admin:client";
    private const string URLS = "URLS";

    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the name to use for this application when registering with SBA.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the base path SBA should use for interacting with your application.
    /// </summary>
    public string BasePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether SBA certificates should be validated.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout (in milliseconds) for interactions with SBA.
    /// </summary>
    public int ConnectionTimeoutMS { get; set; } = 100000;

    /// <summary>
    /// Gets or sets metadata to use when registering with SBA.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }

    public SpringBootAdminClientOptions(IConfiguration config, IApplicationInstanceInfo appInfo)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (appInfo is null)
        {
            throw new ArgumentNullException(nameof(appInfo));
        }

        var section = config.GetSection(PREFIX);
        if (section != null)
        {
            section.Bind(this);
        }

        // Require base path to be supplied directly, in the config, or in the app instance info
        BasePath ??= GetBasePath(config) ?? appInfo?.Uris?.FirstOrDefault() ?? throw new NullReferenceException($"Please set {PREFIX}:BasePath in order to register with Spring Boot Admin");
        ApplicationName ??= appInfo.ApplicationNameInContext(SteeltoeComponent.Management);
    }

    private string GetBasePath(IConfiguration config)
    {
        var urlString = config.GetValue<string>(URLS);
        var urls = urlString?.Split(';');
        if (urls?.Length > 0)
        {
            return urls[0];
        }

        return null;
    }
}
