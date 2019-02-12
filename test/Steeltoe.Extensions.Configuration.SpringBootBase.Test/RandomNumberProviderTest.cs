// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomNumber.Test
{
    public class RandomNumberProviderTest
    {
        [Fact]
        public void Constructor__ThrowsIfPrefixNull()
        {
            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RandomNumberProvider(null, null));
        }

        [Fact]
        public void TryGet_Ignores()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("foo:bar", out string value);
            Assert.Null(value);
        }

        [Fact]
        public void TryGet_String()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:string", out string value);
            Assert.NotNull(value);
        }

        [Fact]
        public void TryGet_Uuid()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:uuid", out string value);
            Assert.NotNull(value);
        }

        [Fact]
        public void TryGet_RandomInt()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:int", out string value);
            Assert.NotNull(value);
        }

        [Fact]
        public void TryGet_RandomIntRange()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:int[4,10]", out string value);
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 4, 10);
        }

        [Fact]
        public void TryGet_RandomIntMax()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:int(10)", out string value);
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 0, 10);
        }

        [Fact]
        public void TryGet_RandomLong()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:long", out string value);
            Assert.NotNull(value);
        }

        [Fact]
        public void TryGet_RandomLongRange()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:long[4,10]", out string value);
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 4, 10);
        }

        [Fact]
        public void TryGet_RandomLongMax()
        {
            var prov = new RandomNumberProvider("random:");
            prov.TryGet("random:long(10)", out string value);
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 0, 10);
        }
    }
}
