// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.Endpoint
{
    public interface IEndpointOptions
    {
        [Obsolete("Use EndPointExtensions.IsEnabled(IEndpointOptions options, IManagementOptions mgmtOptions) instead")]
        bool IsEnabled { get; }

        [Obsolete("Use Exposure Options instead.")]
        bool IsSensitive { get; }

        bool? Enabled { get; }

        [Obsolete("Use Exposure Options instead.")]
        bool? Sensitive { get;  }

        [Obsolete("Use EndPointExtensions instead")]
        IManagementOptions Global { get; }

        string Id { get;  }

        string Path { get; }

        Permissions RequiredPermissions { get; }

        bool IsAccessAllowed(Permissions permissions);
    }
}
