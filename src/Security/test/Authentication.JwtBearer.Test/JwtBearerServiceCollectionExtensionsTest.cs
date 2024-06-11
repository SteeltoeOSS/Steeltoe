// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authentication.JwtBearer.Test;

public sealed class JwtBearerServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureJwtBearerForCloudFoundry_AddsExpectedRegistrations()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.ConfigureJwtBearerForCloudFoundry();

        serviceCollection.Should().Contain(service => service.ServiceType == typeof(IHttpClientFactory));
        serviceCollection.Should().Contain(service => service.ImplementationType == typeof(PostConfigureJwtBearerOptions));
    }

    [Fact]
    public void ConfigureJwtBearerForCloudFoundry_ConfiguresHttpClient()
    {
        var serviceCollection = new ServiceCollection();

        ServiceProvider serviceProvider = serviceCollection.ConfigureJwtBearerForCloudFoundry().BuildServiceProvider();
        HttpClient httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(SteeltoeSecurityDefaults.HttpClientName);

        httpClient.Timeout.Should().Be(new TimeSpan(0, 0, 0, 60));
    }
}
