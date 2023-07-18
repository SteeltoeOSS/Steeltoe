// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public class TraceEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public TraceEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TraceEndpointHandler_CallsTraceRepo()
    {
        using var tc = new TestContext(_output);
        var repo = new TestTraceRepo();

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IHttpTraceRepository>(repo);
            services.AddTraceActuatorServices(MediaTypeVersion.V1);
        };

        var ep = tc.GetService<IHttpTraceEndpointHandler>();
        HttpTraceResult result = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(repo.GetTracesCalled);
    }
}
