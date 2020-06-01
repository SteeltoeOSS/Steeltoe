// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

#pragma warning disable CA2235 // Mark all non-serializable fields
        public Mock<HttpContextBase> Context { get; }
#pragma warning restore CA2235 // Mark all non-serializable fields

        [NonSerialized]
        private readonly ActuatorModule _module;

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
