// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.Common;
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
            var stream = TestHelpers.StringToStream(json);
            var ex = Assert.Throws<JsonSerializationException>(() => JsonSerialization.Deserialize<JsonInstanceInfo>(stream));
        }
    }
}
