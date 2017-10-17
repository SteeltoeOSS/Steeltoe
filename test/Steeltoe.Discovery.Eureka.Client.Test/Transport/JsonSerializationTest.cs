//
// Copyright 2015 the original author or authors.
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
//

using Newtonsoft.Json;
using Steeltoe.Discovery.Eureka.Test;
using System.IO;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Transport.Test
{
    public class JsonSerializationTest : AbstractBaseTest
    {
        [Fact]
        public void Deserialize_BadJson_Throws()
        {
            var json = @"
{ 
    'instanceId':'localhost:foo',
    'hostName':'localhost',
    'app':'FOO',
    'ipAddr':'192.168.56.1',
    
";
            Stream stream = TestHelpers.StringToStream(json);
            var ex = Assert.Throws<JsonSerializationException>(() => JsonSerialization.Deserialize<JsonInstanceInfo>(stream));
        }
    }
}
