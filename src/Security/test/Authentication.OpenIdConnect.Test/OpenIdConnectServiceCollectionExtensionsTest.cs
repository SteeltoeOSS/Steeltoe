// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class OpenIdConnectServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureOpenIdConnectForCloudFoundry_AddsExpectedRegistrations()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.ConfigureOpenIdConnectForCloudFoundry();

        serviceCollection.Should().Contain(service => service.ImplementationType == typeof(PostConfigureOpenIdConnectOptions));
    }
}
