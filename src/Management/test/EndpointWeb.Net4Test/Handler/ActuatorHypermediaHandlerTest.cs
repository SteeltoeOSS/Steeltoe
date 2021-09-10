// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Management.Endpoint.Handler;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Specialized;
using System.Web;
using Xunit;

namespace Steeltoe.Management.EndpointWeb.Handler.Test
{
    public class ActuatorHypermediaHandlerTest
    {
        [Theory]
        [InlineData("http://somehost:1234/somepath", "https://somehost:1234/somepath", "https")]
        [InlineData("http://somehost:443/somepath", "https://somehost/somepath", "https")]
        [InlineData("http://somehost:80/somepath", "http://somehost/somepath", "http")]
        [InlineData("http://somehost:8080/somepath", "http://somehost:8080/somepath", "http")]
        public void GetRequestUri_HandlesPorts(string requestUri, string expectedResult, string xForwarded)
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            var request = new Mock<HttpRequestBase>();
            request.Setup(req => req.Url).Returns(new Uri(requestUri));
            request.Setup(req => req.Path).Returns("/somepath");
            request.Setup(req => req.Headers).Returns(new NameValueCollection { { "X-Forwarded-Proto", xForwarded } });
            var mockEndpoint = new Mock<ActuatorEndpoint>(new Mock<IActuatorHypermediaOptions>().Object, null, null).Object;
            var handler = new ActuatorHypermediaHandler(mockEndpoint, null, null, null);

            // act
            var result = handler.GetRequestUri(request.Object);

            // assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("http://somehost:1234/somepath", "https://somehost/somepath", "https")]
        [InlineData("http://somehost:8080/somepath", "http://somehost/somepath", "http")]
        public void GetRequestUri_IgnoresPortsOnCF(string requestUri, string expectedResult, string xForwarded)
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "notnull");
            var request = new Mock<HttpRequestBase>();
            request.Setup(req => req.Url).Returns(new Uri(requestUri));
            request.Setup(req => req.Path).Returns("/somepath");
            request.Setup(req => req.Headers).Returns(new NameValueCollection { { "X-Forwarded-Proto", xForwarded } });
            var mockEndpoint = new Mock<ActuatorEndpoint>(new Mock<IActuatorHypermediaOptions>().Object, null, null).Object;
            var handler = new ActuatorHypermediaHandler(mockEndpoint, null, null, null);

            // act
            var result = handler.GetRequestUri(request.Object);

            // assert
            Assert.Equal(expectedResult, result);
        }

    }
}
