// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Trace;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Trace;

public sealed class TraceEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task TraceEndpointHandler_CallsTraceRepo()
    {
        using var testContext = new TestContext(_testOutputHelper);
        var repository = new TestTraceRepository();

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IHttpTraceRepository>(repository);
            services.AddTraceActuator(MediaTypeVersion.V1);
        };

        var handler = testContext.GetRequiredService<IHttpTraceEndpointHandler>();
        HttpTraceResult result = await handler.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(repository.GetTracesCalled);
    }
}
