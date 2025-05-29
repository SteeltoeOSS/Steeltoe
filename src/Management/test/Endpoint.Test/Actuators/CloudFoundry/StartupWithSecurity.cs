// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class StartupWithSecurity
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHttpClientFactory>(_ => new MockHttpClientFactory(CloudControllerPermissionsMock.GetHttpMessageHandler()));

        services.AddCloudFoundryActuator();
        services.AddHypermediaActuator();
        services.AddInfoActuator();
    }

    public void Configure(IApplicationBuilder app)
    {
    }

    private sealed class MockHttpClientFactory(MockHttpMessageHandler mockHttpMessageHandler) : IHttpClientFactory
    {
        private readonly MockHttpMessageHandler _mockHttpMessageHandler =
            mockHttpMessageHandler ?? throw new ArgumentNullException(nameof(mockHttpMessageHandler));

        public HttpClient CreateClient(string name)
        {
            return _mockHttpMessageHandler.ToHttpClient();
        }
    }
}
