// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryClaimActionExtensionsTest
{
    [Fact]
    public void MapScopes_AddsClaimAction()
    {
        var col = new ClaimActionCollection();
        col.MapScopes();
        Assert.Single(col);
        Assert.IsType<CloudFoundryScopeClaimAction>(col.FirstOrDefault());
    }
}