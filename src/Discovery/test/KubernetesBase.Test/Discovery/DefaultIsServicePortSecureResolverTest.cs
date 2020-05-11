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

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Discovery.KubernetesBase.Discovery;
using Xunit;

namespace Steeltoe.Discovery.KubernetesBase.Test.Discovery
{
    public class DefaultIsServicePortSecureResolverTest
    {
        [Fact]
        public void PortNumbers_ShouldBeSecuredIfDefaultOrAdded()
        {
            // arrange
            KubernetesDiscoveryOptions properties = new KubernetesDiscoveryOptions();
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
            Assert.True(sut.Resolve(new Input("dummy", 8080,
                new Dictionary<string, string>
                {
                    {"secured", "true"},
                    {"other", "value"}
                })));
            
            Assert.True(sut.Resolve(new Input("dummy", 8080,
                new Dictionary<string, string>
                {
                    {"secured", "1"},
                    {"other", "value"}
                })));
        }

        [Fact]
        public void InputWithSecuredAnnotation_ShouldResolveToTrue()
        {
            // arrange
            var sut = new DefaultIsServicePortSecureResolver(new KubernetesDiscoveryOptions());
            
            // act & assert
            Assert.True(sut.Resolve(new Input("dummy", 8080,
                null,
                new Dictionary<string, string>
                {
                    {"secured", "true"},
                    {"other", "value"}
                })));
            
            Assert.True(sut.Resolve(new Input("dummy", 8080,
                new Dictionary<string, string>
                {
                    {"secured", "1"},
                    {"other", "value"}
                })));
        }
    }
}