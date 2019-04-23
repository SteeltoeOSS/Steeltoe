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

using Moq;
using Steeltoe.Management.Endpoint.Module;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.EndpointWeb.Test
{
    [Serializable]
    public class TestHttpClient
    {
        public TestHttpClient(ActuatorModule module, Mock<HttpContextBase> mockContext)
        {
            _module = module;
            Context = mockContext;
        }

        public Mock<HttpContextBase> Context { get; }

        private ActuatorModule _module;

        public NameValueCollection RequestHeaders { get; set; }

        public async Task<TestResponse> GetAsync(string url, string httpMethod)
        {
            var uri = new Uri(url);
            var response = string.Empty;
            var responseHeaders = new NameValueCollection();
            var requestHeader = new NameValueCollection();
            HttpStatusCode statusCode = HttpStatusCode.OK;

            Context.Setup(ctx => ctx.Request.HttpMethod).Returns(httpMethod);
            Context.Setup(ctx => ctx.Request.Path).Returns(uri.AbsolutePath);
            Context.Setup(ctx => ctx.Request.Url).Returns(uri);
            Context.Setup(ctx => ctx.Request.Headers).Returns(RequestHeaders ?? new NameValueCollection());

            Context.Setup(ctx => ctx.Response.Headers).Returns(responseHeaders ?? new NameValueCollection());
            Context.SetupSet(ctx => ctx.Response.StatusCode = It.IsAny<int>()).Callback((int c) => statusCode = (HttpStatusCode)c);

            Context.Setup(ctx => ctx.Response.Output.WriteAsync(It.IsAny<string>()))
                .Callback((string s) => response = s)
                .Returns(Task.CompletedTask);

            Context.Setup(ctx => ctx.Response.Write(It.IsAny<string>())).Callback((string s) => response = s);

             await _module.FilterAndPreProcessRequest(Context.Object, () => { });

            return new TestResponse(response, statusCode, responseHeaders);
        }
    }
}
