// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;

namespace Steeltoe.Security.Authorization.Certificate;

/// <summary>
/// Client certificates must originate in the same space as the authorizing application.
/// </summary>
public sealed class SameSpaceRequirement : IAuthorizationRequirement
{
}
