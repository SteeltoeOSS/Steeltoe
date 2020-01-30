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
using System;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class CachingDestinationResolverTest
    {
        [Fact]
        public void CachedDestination()
        {
            var resolverMock = new Mock<IDestinationResolver<string>>();
            var resolver = resolverMock.Object;
            var resolverProxy = new CachingDestinationResolverProxy<string>(resolver);
            resolverMock.Setup(r => r.ResolveDestination("abcd")).Returns("dcba");
            resolverMock.Setup(r => r.ResolveDestination("1234")).Returns("4321");

            Assert.Equal("dcba", resolverProxy.ResolveDestination("abcd"));
            Assert.Equal("4321", resolverProxy.ResolveDestination("1234"));
            Assert.Equal("4321", resolverProxy.ResolveDestination("1234"));
            Assert.Equal("dcba", resolverProxy.ResolveDestination("abcd"));

            resolverMock.Verify(r => r.ResolveDestination("abcd"), Times.Once);
            resolverMock.Verify(r => r.ResolveDestination("1234"), Times.Once);
        }

        [Fact]
        public void NullTargetThroughConstructor()
        {
            Assert.Throws<ArgumentNullException>(() => new CachingDestinationResolverProxy<string>(null));
        }
    }
}
