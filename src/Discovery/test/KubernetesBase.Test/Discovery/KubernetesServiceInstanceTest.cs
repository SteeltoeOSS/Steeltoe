// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using Steeltoe.Discovery.Kubernetes.Discovery;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Kubernetes.Test.Discovery
{
    public class KubernetesServiceInstanceTest
    {
        [Fact]
        public void SchemeIsHttp()
        {
            AssertServiceInstance(false);
        }

        [Fact]
        public void SchemeIsHttps()
        {
            AssertServiceInstance(true);
        }

        private KubernetesServiceInstance AssertServiceInstance(bool secure)
        {
            var address = new V1EndpointAddress { Ip = "1.2.3.4" };
            var port = new V1EndpointPort { Port = 8080 };
            var instance = new KubernetesServiceInstance("123", "myString", address, port, new Dictionary<string, string>(), secure);
            Assert.Equal(expected: "123", actual: instance.InstanceId);
            Assert.Equal(expected: "myString", actual: instance.ServiceId);
            Assert.Equal(expected: "1.2.3.4", actual: instance.Host);
            Assert.Equal(expected: 8080, actual: instance.Port);
            Assert.Equal(expected: secure, actual: instance.IsSecure);
            Assert.Equal(expected: secure ? "https" : "http", actual: instance.GetScheme());
            return instance;
        }
    }
}