// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test;

internal class MockKubeApiServer : IDisposable
{
    private readonly IWebHost _webHost;

    private readonly string _configMapResponse = "{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"TestValue\"}}\n";

    private readonly string _secretResponse = "{\"kind\":\"Secret\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testsecret\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/secrets/testsecret\",\"uid\":\"04a256d5-5480-4e6a-ab1a-81b1df2b1f15\",\"resourceVersion\":\"724153\",\"creationTimestamp\":\"2020-04-17T14:32:42Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"testKey\\\":\\\"dGVzdFZhbHVl\\\"},\\\"kind\\\":\\\"Secret\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"testsecret\\\",\\\"namespace\\\":\\\"default\\\"},\\\"type\\\":\\\"Opaque\\\"}\\n\"}},\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"type\":\"Opaque\"}\n";

    public MockKubeApiServer(Func<HttpContext, Task<bool>> shouldNext = null)
    {
        shouldNext ??= _ => Task.FromResult(true);

        _webHost = WebHost.CreateDefaultBuilder()
            .Configure(app => app.Run(async httpContext =>
            {
                if (await shouldNext(httpContext))
                {
                    if (httpContext.Request.Path.Equals("/api/v1/namespaces/default/configmaps"))
                    {
                        await httpContext.Response.WriteAsync(_configMapResponse);
                    }
                    else if (httpContext.Request.Path.Equals("/api/v1/namespaces/default/secrets"))
                    {
                        await httpContext.Response.WriteAsync(_secretResponse);
                    }
                }
            }))
            .UseKestrel(options => { options.Listen(IPAddress.Loopback, 0); })
            .Build();

        _webHost.Start();
    }

    public Uri Uri => _webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Select(a => new Uri(a)).First();

    public void Dispose()
    {
        _webHost.StopAsync();
        _webHost.WaitForShutdown();
    }
}
