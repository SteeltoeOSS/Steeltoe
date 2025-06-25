// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Used to detect if the Cloud Foundry security middleware has been added to the pipeline.
/// </summary>
internal sealed class HasCloudFoundrySecurityMiddlewareMarker
{
    public bool Value { get; set; }
}
