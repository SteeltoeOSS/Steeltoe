// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    public void Constructor_ThrowsIfNullRepo()
    {
        IOptionsMonitor<TraceEndpointOptions> opts = GetOptionsMonitorFromSettings<TraceEndpointOptions>();
        Assert.Throws<ArgumentNullException>(() => new TraceEndpointHandler(opts, null, null));
        Assert.Throws<ArgumentNullException>(() => new TraceEndpointHandler(opts, new TestTraceRepository(), null));
    }

    [Fact]
    public async Task TraceEndpointHandler_CallsTraceRepo()
    {
        using var tc = new TestContext(_output);
        var repo = new TestTraceRepo();

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<ITraceRepository>(repo);
            services.AddTraceActuatorServices(MediaTypeVersion.V1);
        };

        var ep = tc.GetService<ITraceEndpointHandler>();
        IList<TraceResult> result = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(repo.GetTracesCalled);
    }

    private sealed class TestTraceRepository : ITraceRepository
    {
        public IList<TraceResult> GetTraces()
        {
            throw new NotImplementedException();
        }
    }
}
