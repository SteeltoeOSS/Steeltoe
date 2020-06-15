// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Test
{
    internal class TestOptions : IEndpointOptions
    {
        public string Id { get; set; }

        public bool? Enabled { get; set; }

        public IManagementOptions Global { get; set; }

        public string Path { get; set; }

        public Permissions RequiredPermissions { get; set; }

        public bool IsEnabled => Enabled.Value;

        public bool IsSensitive => throw new System.NotImplementedException();

        public bool? Sensitive => throw new System.NotImplementedException();

        public bool IsAccessAllowed(Permissions permissions)
        {
            return false;
        }
    }
}
