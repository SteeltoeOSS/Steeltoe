// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Env;

public class EnvEndpointOptions : AbstractEndpointOptions, IEnvOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:env";

    private static readonly string[] DefaultKeysToSanitize =
    {
        "password",
        "secret",
        "key",
        "token",
        ".*credentials.*",
        "vcap_services"
    };

    public string[] KeysToSanitize { get; set; }

    public EnvEndpointOptions()
    {
        Id = "env";
        RequiredPermissions = Permissions.Restricted;
        KeysToSanitize = DefaultKeysToSanitize;
    }

    public EnvEndpointOptions(IConfiguration configuration)
        : base(ManagementInfoPrefix, configuration)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "env";
        }

        if (RequiredPermissions == Permissions.Undefined)
        {
            RequiredPermissions = Permissions.Restricted;
        }

        KeysToSanitize ??= DefaultKeysToSanitize;
    }
}
