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
using k8s.Models;
using Steeltoe.Discovery.KubernetesBase.Discovery;
using Xunit;

namespace Steeltoe.Discovery.KubernetesBase.Test.Discovery
{
    public class KubernetesServiceInstanceTest
    {
        private KubernetesServiceInstance assertServiceInstance(bool secure)
        {
            V1EndpointAddress address = new V1EndpointAddress
            {
                Ip = "1.2.3.4"
            };
            V1EndpointPort port = new V1EndpointPort
            {
                Port = 8080
            };
            KubernetesServiceInstance instance = new KubernetesServiceInstance("123",
                "myString", address, port, new Dictionary<string, string>(),secure);
            Assert.Equal(expected: "123", actual: instance.InstanceId);
            Assert.Equal(expected: "myString", actual: instance.ServiceId);
            Assert.Equal(expected: "1.2.3.4", actual: instance.Host);
            Assert.Equal(expected: 8080, actual: instance.Port);
            Assert.Equal(expected: secure, actual: instance.IsSecure);
            Assert.Equal(expected: secure ? "https" : "http", actual: instance.GetScheme());
            return instance;
        }

        [Fact]
        public void SchemeIsHttp()
        {
            assertServiceInstance(false);
        }

        [Fact]
        public void SchemeIsHttps()
        {
            assertServiceInstance(true);
        }
        
    }
}