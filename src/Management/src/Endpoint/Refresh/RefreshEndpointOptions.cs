// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Refresh;

public class RefreshEndpointOptions : AbstractEndpointOptions, IRefreshOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:refresh";
    private const bool DefaultReturnConfiguration = true;

    private bool? _returnConfiguration;

    public bool ReturnConfiguration
    {
        get => _returnConfiguration ?? DefaultReturnConfiguration;
        set => _returnConfiguration = value;
    }

    public RefreshEndpointOptions()
    {
        Id = "refresh";
        RequiredPermissions = Permissions.Restricted;
        _returnConfiguration = DefaultReturnConfiguration;
    }

    public RefreshEndpointOptions(IConfiguration configuration)
        : base(ManagementInfoPrefix, configuration)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "refresh";
        }

        if (RequiredPermissions == Permissions.Undefined)
        {
            RequiredPermissions = Permissions.Restricted;
        }

        _returnConfiguration ??= DefaultReturnConfiguration;
    }
}
