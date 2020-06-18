// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Management
{
    public interface IEndpointOptions
    {
        bool? Enabled { get; }

        string Id { get; }

        string Path { get; }

        Permissions RequiredPermissions { get; }

        bool IsAccessAllowed(Permissions permissions);

        public IEnumerable<string> AllowedVerbs { get; set; }

        public bool ExactMatch { get; set; }
    }
}
