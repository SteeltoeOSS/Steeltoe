// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class HttpExchangesEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task HttpExchangeEndpointHandler_CallsHttpExchangeRepository()
    {
        using var testContext = new TestContext(_testOutputHelper);
        var repository = new TestHttpExchangesRepository();

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IHttpExchangesRepository>(repository);
            services.AddHttpExchangesActuator();
        };

        var handler = testContext.GetRequiredService<IHttpExchangesEndpointHandler>();
        HttpExchangesResult result = await handler.InvokeAsync(null, CancellationToken.None);
        result.Should().NotBeNull();
        repository.GetHttpExchangesCalled.Should().BeTrue();
    }
}
