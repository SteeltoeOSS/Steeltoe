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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Test.Options
{
    public class AbstractOptionsTest
    {
        [Fact]
        public void Constructors_ThrowsOnNulls()
        {
            IConfigurationRoot root = null;
            IConfiguration config = null;

            Assert.Throws<ArgumentNullException>(() => new TestOptions(root, "foobar"));
            Assert.Throws<ArgumentNullException>(() => new TestOptions(config));
        }

        [Fact]
        public void Constructors_BindsValues()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "foo", "bar" }
            });

            IConfigurationRoot root = builder.Build();
            IConfiguration config = root;

            var opt1 = new TestOptions(root);
            Assert.Equal("bar", opt1.Foo);

            var opt2 = new TestOptions(config);
            Assert.Equal("bar", opt2.Foo);

            var builder2 = new ConfigurationBuilder();
            builder2.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "prefix:foo", "bar" }
            });
            IConfigurationRoot root2 = builder2.Build();
            var opt3 = new TestOptions(root2, "prefix");
            Assert.Equal("bar", opt3.Foo);
        }
    }
}
