// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

internal sealed class InstanceInfoBuilder
{
    private readonly string _hostName = "localhost";
    private readonly string _ipAddress = "127.0.0.1";
    private readonly DataCenterInfo _dataCenterInfo = new();
    private string _appName = "FOOBAR";
    private string? _instanceId;
    private InstanceStatus? _status;
    private string? _vipAddress;
    private string? _secureVipAddress;
    private ActionType? _actionType;

    public InstanceInfo Build()
    {
        string id = _instanceId ?? $"{_hostName}:{_appName}:1234";

        return new InstanceInfo(id, _appName, _hostName, _ipAddress, _dataCenterInfo)
        {
            Status = _status,
            VipAddress = _vipAddress,
            SecureVipAddress = _secureVipAddress,
            ActionType = _actionType
        };
    }

    public InstanceInfoBuilder WithId(string instanceId)
    {
        _instanceId = instanceId;
        return this;
    }

    public InstanceInfoBuilder WithAppName(string appName)
    {
        _appName = appName;
        return this;
    }

    public InstanceInfoBuilder WithStatus(InstanceStatus? status)
    {
        _status = status;
        return this;
    }

    public InstanceInfoBuilder WithVipAddress(string? vipAddress)
    {
        _vipAddress = vipAddress;
        return this;
    }

    public InstanceInfoBuilder WithSecureVipAddress(string? secureVipAddress)
    {
        _secureVipAddress = secureVipAddress;
        return this;
    }

    public InstanceInfoBuilder WithActionType(ActionType? actionType)
    {
        _actionType = actionType;
        return this;
    }
}
