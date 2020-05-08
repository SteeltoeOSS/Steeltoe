// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    internal class MockKubeApiServer : IDisposable
    {
        private readonly IWebHost _webHost;

        private readonly string configMapResponse = "{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"TestValue\"}}\n";

        private readonly string secretResponse = "{\"kind\":\"Secret\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testsecret\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/secrets/testsecret\",\"uid\":\"04a256d5-5480-4e6a-ab1a-81b1df2b1f15\",\"resourceVersion\":\"724153\",\"creationTimestamp\":\"2020-04-17T14:32:42Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"testKey\\\":\\\"dGVzdFZhbHVl\\\"},\\\"kind\\\":\\\"Secret\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"testsecret\\\",\\\"namespace\\\":\\\"default\\\"},\\\"type\\\":\\\"Opaque\\\"}\\n\"}},\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"type\":\"Opaque\"}\n";

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
                            await httpContext.Response.WriteAsync(configMapResponse);
                        }
                        else if (httpContext.Request.Path.Equals("/api/v1/namespaces/default/secrets"))
                        {
                            await httpContext.Response.WriteAsync(secretResponse);
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
            _webHost.Dispose();
        }
    }
}
