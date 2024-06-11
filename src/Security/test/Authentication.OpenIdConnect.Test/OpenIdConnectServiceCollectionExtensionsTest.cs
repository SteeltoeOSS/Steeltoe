// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class OpenIdConnectServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureOpenIdConnectForCloudFoundry_AddsExpectedRegistrations()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.ConfigureOpenIdConnectForCloudFoundry();

        serviceCollection.Should().Contain(service => service.ServiceType == typeof(IHttpClientFactory));
        serviceCollection.Should().Contain(service => service.ImplementationType == typeof(PostConfigureOpenIdConnectOptions));
    }

    [Fact]
    public void ConfigureOpenIdConnectForCloudFoundry_RequiredBeforeAddJwtBearer()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAuthentication().AddOpenIdConnect();
        var exception = Assert.Throws<InvalidOperationException>(() => serviceCollection.ConfigureOpenIdConnectForCloudFoundry());
        exception.Message.Should().Contain($"{nameof(OpenIdConnectServiceCollectionExtensions.ConfigureOpenIdConnectForCloudFoundry)} must be called before");
    }

    [Fact]
    public void ConfigureOpenIdConnectForCloudFoundry_ConfiguresHttpClient()
    {
        var serviceCollection = new ServiceCollection();

        ServiceProvider serviceProvider = serviceCollection.ConfigureOpenIdConnectForCloudFoundry().BuildServiceProvider();
        HttpClient httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(SteeltoeSecurityDefaults.HttpClientName);

        httpClient.Timeout.Should().Be(new TimeSpan(0, 0, 0, 60));
    }
}
