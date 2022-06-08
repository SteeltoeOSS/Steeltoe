// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class ApplicationClaimTypes
{
    public const string CloudFoundryOrgId = "CloudFoundryOrgId";
    public const string CloudFoundrySpaceId = "CloudFoundrySpaceId";
    public const string CloudFoundryAppId = "CloudFoundryAppId";
    public const string CloudFoundryInstanceId = "CloudFoundryContainerInstanceId";
}
