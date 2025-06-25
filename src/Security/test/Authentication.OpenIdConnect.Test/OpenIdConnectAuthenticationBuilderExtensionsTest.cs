// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class OpenIdConnectAuthenticationBuilderExtensionsTest
{
    [Fact]
    public void ConfigureOpenIdConnectForCloudFoundry_AddsExpectedRegistrations()
    {
        AuthenticationBuilder authenticationBuilder = new ServiceCollection().AddAuthentication().AddOpenIdConnect();

        authenticationBuilder.ConfigureOpenIdConnectForCloudFoundry();

        authenticationBuilder.Services.Should().Contain(service => service.ImplementationType == typeof(PostConfigureOpenIdConnectOptions));
    }
}
