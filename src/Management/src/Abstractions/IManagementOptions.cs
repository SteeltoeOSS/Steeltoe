// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management;

public interface IManagementOptions
{
    bool? Enabled { get; }

    string Path { get; }

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    List<IEndpointOptions> EndpointOptions { get; }
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs

    public bool UseStatusCodeFromResponse { get; set; }
}
