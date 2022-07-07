// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Env;

public class EnvEndpointOptions : AbstractEndpointOptions, IEnvOptions
{
    private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:env";
    private static readonly string[] KEYS_TO_SANITIZE = { "password", "secret", "key", "token", ".*credentials.*", "vcap_services" };

    public EnvEndpointOptions()
    {
        Id = "env";
        RequiredPermissions = Permissions.RESTRICTED;
        KeysToSanitize = KEYS_TO_SANITIZE;
    }

    public EnvEndpointOptions(IConfiguration config)
        : base(MANAGEMENT_INFO_PREFIX, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "env";
        }

        if (RequiredPermissions == Permissions.UNDEFINED)
        {
            RequiredPermissions = Permissions.RESTRICTED;
        }

        KeysToSanitize ??= KEYS_TO_SANITIZE;
    }

    public string[] KeysToSanitize { get; set; }
}
