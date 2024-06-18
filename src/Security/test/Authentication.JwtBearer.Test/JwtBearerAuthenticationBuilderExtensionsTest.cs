// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Security.Authentication.JwtBearer.Test;

public sealed class JwtBearerAuthenticationBuilderExtensionsTest
{
    [Fact]
    public void ConfigureJwtBearerForCloudFoundry_AddsExpectedRegistrations()
    {
        AuthenticationBuilder authenticationBuilder = new ServiceCollection().AddAuthentication().AddJwtBearer();

        authenticationBuilder.ConfigureJwtBearerForCloudFoundry();

        authenticationBuilder.Services.Should().Contain(service => service.ImplementationType == typeof(PostConfigureJwtBearerOptions));
    }
}
