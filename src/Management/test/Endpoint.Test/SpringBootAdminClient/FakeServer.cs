// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

internal sealed class FakeServer : IServer
{
    public IFeatureCollection Features { get; } = new FeatureCollection();

    public FakeServer(IConfiguration configuration)
    {
        string? urls = configuration.GetValue<string>("urls");

        Features.Set<IServerAddressesFeature>(urls != null
            ? new FakeServerAddressesFeature(urls.Split(';'))
            : new FakeServerAddressesFeature(["http://localhost:5000"]));
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    private sealed class FakeServerAddressesFeature(ICollection<string> addresses) : IServerAddressesFeature
    {
        public ICollection<string> Addresses { get; } = addresses;
        public bool PreferHostingUrls { get; set; }
    }
}
