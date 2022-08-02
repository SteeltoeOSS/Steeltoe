// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management;

public interface IEndpointOptions
{
    bool? Enabled { get; }

    string Id { get; }

    string Path { get; }

    Permissions RequiredPermissions { get; }

    IEnumerable<string> AllowedVerbs { get; }

    bool ExactMatch { get; }

    bool IsAccessAllowed(Permissions permissions);
}
