// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.KubernetesBase.Discovery;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.KubernetesBase.Test.Discovery
{
    public class DefaultIsServicePortSecureResolverTest
    {
        [Fact]
        public void PortNumbers_ShouldBeSecuredIfDefaultOrAdded()
        {
            // arrange
            var properties = new KubernetesDiscoveryOptions();
            properties.KnownSecurePorts.Add(12345);
            var sut = new DefaultIsServicePortSecureResolver(properties);

            // act & assert
            Assert.False(sut.Resolve(new Input("dummy")));
            Assert.False(sut.Resolve(new Input("dummy", 8080)));
            Assert.False(sut.Resolve(new Input("dummy", 1234)));

            Assert.True(sut.Resolve(new Input("dummy", 443)));
            Assert.True(sut.Resolve(new Input("dummy", 8443)));
            Assert.True(sut.Resolve(new Input("dummy", 12345)));
        }

        [Fact]
        public void InputWithSecuredLabel_ShouldResolveToTrue()
        {
            // arrange
            var sut = new DefaultIsServicePortSecureResolver(new KubernetesDiscoveryOptions());

            // act & assert
            Assert.True(sut.Resolve(new Input("dummy", 8080, new Dictionary<string, string> { { "secured", "true" }, { "other", "value" } })));

            Assert.True(sut.Resolve(new Input("dummy", 8080, new Dictionary<string, string> { { "secured", "1" }, { "other", "value" } })));
        }

        [Fact]
        public void InputWithSecuredAnnotation_ShouldResolveToTrue()
        {
            // arrange
            var sut = new DefaultIsServicePortSecureResolver(new KubernetesDiscoveryOptions());

            // act & assert
            Assert.True(sut.Resolve(new Input("dummy", 8080, null, new Dictionary<string, string> { { "secured", "true" }, { "other", "value" } })));

            Assert.True(sut.Resolve(new Input("dummy", 8080, new Dictionary<string, string> { { "secured", "1" }, { "other", "value" } })));
        }
    }
}