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

namespace Steeltoe.Management.Endpoint.Env.Test
{
    public class PropertySourceDescriptorTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var properties = new Dictionary<string, PropertyValueDescriptor>()
            {
                { "key1", new PropertyValueDescriptor("value") },
                { "key2", new PropertyValueDescriptor(false) },
            };
            var propDesc = new PropertySourceDescriptor("name", properties);
            Assert.Equal("name", propDesc.Name);
            Assert.Same(properties, propDesc.Properties);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var properties = new Dictionary<string, PropertyValueDescriptor>()
            {
                { "key1", new PropertyValueDescriptor("value") },
                { "key2", new PropertyValueDescriptor(false) },
            };
            var propDesc = new PropertySourceDescriptor("name", properties);
            var result = Serialize(propDesc);
            Assert.Equal("{\"properties\":{\"key1\":{\"value\":\"value\"},\"key2\":{\"value\":false}},\"name\":\"name\"}", result);
        }
    }
}
