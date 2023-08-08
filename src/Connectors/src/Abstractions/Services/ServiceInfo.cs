// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Connectors.Services;

internal abstract class ServiceInfo : IServiceInfo
{
    public string Id { get; }

    public IApplicationInstanceInfo ApplicationInfo { get; set; }

    protected ServiceInfo(string id)
        : this(id, null)
    {
    }

    protected ServiceInfo(string id, IApplicationInstanceInfo info)
    {
        ArgumentGuard.NotNullOrEmpty(id);

        Id = id;
        ApplicationInfo = info;
    }
}
