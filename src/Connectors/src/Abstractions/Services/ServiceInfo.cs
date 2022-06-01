// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;

namespace Steeltoe.Connector.Services;

public abstract class ServiceInfo : IServiceInfo
{
    protected ServiceInfo(string id)
        : this(id, null)
    {
    }

    protected ServiceInfo(string id, IApplicationInstanceInfo info)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        Id = id;
        ApplicationInfo = info;
    }

    public string Id { get; protected set; }

    public IApplicationInstanceInfo ApplicationInfo { get; set; }
}
