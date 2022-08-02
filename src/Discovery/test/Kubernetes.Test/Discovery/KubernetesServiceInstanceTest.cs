// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using Steeltoe.Discovery.Kubernetes.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Kubernetes.Test.Discovery;

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

    private void AssertServiceInstance(bool secure)
    {
        var address = new V1EndpointAddress
        {
            Ip = "1.2.3.4"
        };

        var port = new Corev1EndpointPort
        {
            Port = 8080
        };

        var instance = new KubernetesServiceInstance("123", "myString", address, port, new Dictionary<string, string>(), secure);
        Assert.Equal("123", instance.InstanceId);
        Assert.Equal("myString", instance.ServiceId);
        Assert.Equal("1.2.3.4", instance.Host);
        Assert.Equal(8080, instance.Port);
        Assert.Equal(secure, instance.IsSecure);
        Assert.Equal(secure ? "https" : "http", instance.GetScheme());
    }
}
