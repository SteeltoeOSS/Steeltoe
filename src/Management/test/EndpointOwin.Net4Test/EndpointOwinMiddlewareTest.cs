// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using Moq;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.EndpointOwin.Hypermedia;
using Steeltoe.Management.EndpointOwin.Hypermedia.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Test
{
    public class EndpointOwinMiddlewareTest
    {
        [Theory]
        [InlineData("http://somehost:1234/somepath", "https://somehost:1234/somepath", "https")]
        [InlineData("http://somehost:443/somepath", "https://somehost/somepath", "https")]
        [InlineData("http://somehost:80/somepath", "http://somehost/somepath", "http")]
        [InlineData("http://somehost:8080/somepath", "http://somehost:8080/somepath", "http")]
        public void GetRequestUri_HandlesPorts(string requestUriString, string expectedResult, string xForwarded)
        {
            // arrange
            var requestUri = new Uri(requestUriString);
            var request = new Mock<IOwinRequest>();
            request.Setup(req => req.Uri).Returns(requestUri);
            request.Setup(req => req.Host).Returns(new HostString($"{requestUri.Host}:{requestUri.Port}"));
            request.Setup(req => req.LocalPort).Returns(requestUri.Port);
            request.Setup(req => req.Path).Returns(new PathString(requestUri.PathAndQuery));
            request.Setup(req => req.Headers).Returns(new HeaderDictionary(new Dictionary<string, string[]>() { { "X-Forwarded-Proto", new string[] { xForwarded } } }));
            var mockEndpoint = new Mock<ActuatorEndpoint>(new Mock<IActuatorHypermediaOptions>().Object, null, null).Object;
            var mgmtOptions = new List<IManagementOptions> { new ActuatorManagementOptions() };
            var mw = new ActuatorHypermediaEndpointOwinMiddleware(null, new TestActuatorHypermediaEndpoint(new HypermediaEndpointOptions(), mgmtOptions), mgmtOptions);

            // act
            var result = mw.GetRequestUri(request.Object);

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
}
