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

using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoBuilderTest : BaseTest
    {
        [Fact]
        public void ReturnsEmptyDictionary()
        {
            var builder = new InfoBuilder();
            var built = builder.Build();
            Assert.NotNull(built);
            Assert.Empty(built);
        }

        [Fact]
        public void WithInfoSingleValueAddsValue()
        {
            var builder = new InfoBuilder();
            builder.WithInfo("foo", "bar");
            var built = builder.Build();
            Assert.NotNull(built);
            Assert.Single(built);
            Assert.Equal("bar", built["foo"]);
        }

        [Fact]
        public void WithInfoDictionaryAddsValues()
        {
            var builder = new InfoBuilder();
            Dictionary<string, object> items = new Dictionary<string, object>()
            {
                { "foo", "bar" },
                { "bar", 100 }
            };
            builder.WithInfo(items);
            var built = builder.Build();
            Assert.NotNull(built);
            Assert.Equal(2, built.Count);
            Assert.Equal("bar", built["foo"]);
            Assert.Equal(100, built["bar"]);
        }
    }
}
