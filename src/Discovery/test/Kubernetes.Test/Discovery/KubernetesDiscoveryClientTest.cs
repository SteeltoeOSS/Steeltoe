// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using RichardSzalay.MockHttp;
using Steeltoe.Common;
using Steeltoe.Discovery.Kubernetes.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Discovery.Kubernetes.Test.Discovery;

public class KubernetesDiscoveryClientTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, mockHttpMessageHandler.ToHttpClient());
        const string expectedDesc = "Steeltoe provided Kubernetes native service discovery client";
        var k8SDiscoveryOptions = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions());

        var testK8SDiscoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(k8SDiscoveryOptions.CurrentValue),
            client,
            k8SDiscoveryOptions);

        Assert.Equal(expectedDesc, testK8SDiscoveryClient.Description);
    }

    [Fact]
    public void GetInstances_ThrowsOnNull()
    {
        var k8SDiscoveryOptions = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions());
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" });
        var testK8SDiscoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(k8SDiscoveryOptions.CurrentValue),
            client,
            k8SDiscoveryOptions);

        Assert.Throws<ArgumentNullException>(() => testK8SDiscoveryClient.GetInstances(null));
    }

    [Fact]
    public void GetInstances_ShouldBeAbleToHandleEndpointsFromMultipleNamespaces()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "/api/v1/endpoints?fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent("{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Endpoints\",\"metadata\":{\"name\":\"endpoint\",\"namespace\":\"test\"},\"subsets\":[{\"addresses\":[{\"ip\":\"ip1\", \"targetRef\": {\"uid\":\"uid1\"}}],\"ports\":[{\"name\":\"http\",\"port\":80,\"protocol\":\"TCP\"}]}]},{\"apiVersion\":\"v1\",\"kind\":\"Endpoints\",\"metadata\":{\"name\":\"endpoint\",\"namespace\":\"test2\"},\"subsets\":[{\"addresses\":[{\"ip\":\"ip2\",\"targetRef\": {\"uid\":\"uid2\"}}],\"ports\":[{\"name\":\"http\",\"port\":80,\"protocol\":\"TCP\"}]}]}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"l\":\"v\"},\"name\":\"endpoint\",\"namespace\":\"test\",\"uid\":\"uids1\"}}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test2/services")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"l\":\"v\"},\"name\":\"endpoint\",\"namespace\":\"test2\",\"uid\":\"uids2\"}}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions()
        {
            AllNamespaces = true
        });

        IDiscoveryClient discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var genericInstances = discoveryClient.GetInstances("endpoint");
        var instances = genericInstances.Select(s => (KubernetesServiceInstance)s).ToList();

        Assert.NotNull(instances);
        Assert.Equal(actual: instances.Count, expected: 2);

        Assert.Equal(
            actual: instances.Count(s => s.Host.Equals("ip1") && !s.IsSecure),
            expected: 1);
        Assert.Equal(
            actual: instances.Count(s => s.Host.Equals("ip2") && !s.IsSecure),
            expected: 1);
        Assert.Equal(
            actual: instances.Count(s => s.InstanceId.Equals("uid1")),
            expected: 1);
        Assert.Equal(
            actual: instances.Count(s => s.InstanceId.Equals("uid2")),
            expected: 1);
    }

    [Fact]
    public void GetInstances_ShouldBeAbleToHandleEndpointsSingleAddress()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "/api/v1/namespaces/test/endpoints")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent("{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Endpoints\",\"metadata\":{\"name\":\"endpoint\",\"namespace\":\"test\"},\"subsets\":[{\"addresses\":[{\"ip\":\"ip1\", \"targetRef\": {\"uid\":\"uid1\"}}],\"ports\":[{\"name\":\"http\",\"port\":80,\"protocol\":\"TCP\"}]}]}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"l\":\"v\"},\"name\":\"endpoint\",\"namespace\":\"test\",\"uid\":\"uids1\"}}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions()
        {
            Namespace = "test"
        });

        IDiscoveryClient discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var genericInstances = discoveryClient.GetInstances("endpoint");
        var instances = genericInstances.Select(s => (KubernetesServiceInstance)s).ToList();

        Assert.NotNull(instances);
        Assert.Single(instances);
        Assert.Single(instances.Where(i => i.Host.Equals("ip1") && !i.IsSecure));
        Assert.Single(instances.Where(i => i.InstanceId.Equals("uid1")));
    }

    [Fact]
    public void GetInstances_ShouldBeAbleToHandleEndpointsSingleAddressAndMultiplePorts()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "/api/v1/namespaces/test/endpoints")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent("{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Endpoints\",\"metadata\":{\"name\":\"endpoint\",\"namespace\":\"test\"},\"subsets\":[{\"addresses\":[{\"ip\":\"ip1\", \"targetRef\": {\"uid\":\"uid1\"}}],\"ports\":[{\"name\":\"http\",\"port\":80,\"protocol\":\"TCP\"},{\"name\":\"mgmt\",\"port\":9000,\"protocol\":\"TCP\"}]}]}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"l\":\"v\"},\"name\":\"endpoint\",\"namespace\":\"test\",\"uid\":\"uids1\"}}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions()
        {
            Namespace = "test"
        });

        IDiscoveryClient discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var genericInstances = discoveryClient.GetInstances("endpoint");
        var instances = genericInstances.Select(s => (KubernetesServiceInstance)s).ToList();

        Assert.NotNull(instances);
        Assert.Single(instances);
        Assert.Single(instances.Where(i => i.Host.Equals("ip1") && !i.IsSecure));
        Assert.Single(instances.Where(i => i.InstanceId.Equals("uid1")));
        Assert.Single(instances.Where(i => i.Port == 80));
    }

    [Fact]
    public void GetInstances_ShouldBeAbleToHandleEndpointsMultipleAddresses()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "/api/v1/namespaces/test/endpoints")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent("{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Endpoints\",\"metadata\":{\"name\":\"endpoint\",\"namespace\":\"test\"},\"subsets\":[{\"addresses\":[{\"ip\":\"ip1\", \"targetRef\": {\"uid\":\"uid1\"}},{\"ip\":\"ip2\", \"targetRef\": {\"uid\":\"uid2\"}}],\"ports\":[{\"name\":\"https\",\"port\":443,\"protocol\":\"TCP\"}]}]}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .WithQueryString("fieldSelector=metadata.name%3Dendpoint")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"l\":\"v\"},\"name\":\"endpoint\",\"namespace\":\"test\",\"uid\":\"uids1\"}}],\"kind\":\"List\",\"metadata\":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions()
        {
            Namespace = "test"
        });

        IDiscoveryClient discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var instances = discoveryClient.GetInstances("endpoint");

        Assert.NotNull(instances);
        Assert.Equal(expected: 2, actual: instances.Count);
        Assert.Single(instances.Where(i => i.Host.Equals("ip1")).Select(s => s));
        Assert.Single(instances.Where(i => i.Host.Equals("ip2")).Select(s => s));
    }

    [Fact]
    public void GetServices_ShouldReturnAllServicesWhenNoLabelsAreAppliedToTheClient()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":" +
                    "{\"labels\":{\"label1\":\"value1\"},\"name\":\"endpoint1\",\"namespace\":\"test\",\"uid\":" +
                    "\"uids1\"}},{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"label2\":" +
                    "\"value2\"},\"name\":\"endpoint2\",\"namespace\":\"test\",\"uid\":\"uids2\"}},{\"apiVersion\":" +
                    "\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"label3\":\"value3\"},\"name\":" +
                    "\"endpoint3\",\"namespace\":\"test\",\"uid\":\"uids2\"}}],\"kind\":\"List\",\"metadata\"" +
                    ":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions()
        {
            Namespace = "test"
        });

        var discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var services = discoveryClient.Services;

        Assert.NotNull(services);
        Assert.Equal(actual: services.Count, expected: 3);
        Assert.True(services.Contains("endpoint1"));
        Assert.True(services.Contains("endpoint2"));
        Assert.True(services.Contains("endpoint3"));
    }

    [Fact]
    public void GetServices_ShouldReturnOnlyMatchingServicesWhenLabelsAreAppliedToTheClient()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .WithQueryString("labelSelector=label%3Dvalue")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":" +
                    "{\"labels\":{\"label1\":\"value1\"},\"name\":\"endpoint1\",\"namespace\":\"test\",\"uid\":" +
                    "\"uids1\"}},{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"label2\":" +
                    "\"value2\"},\"name\":\"endpoint2\",\"namespace\":\"test\",\"uid\":\"uids2\"}}]," +
                    "\"kind\":\"List\",\"metadata\"" +
                    ":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(new KubernetesDiscoveryOptions()
        {
            Namespace = "test"
        });

        var discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var services = discoveryClient.GetServices(new Dictionary<string, string>
        {
            { "label", "value" }
        });

        Assert.NotNull(services);
        Assert.Equal(actual: services.Count, expected: 2);
        Assert.True(services.Contains("endpoint1"));
        Assert.True(services.Contains("endpoint2"));
    }

    [Fact]
    public void EnabledPropertyWorksBothWays()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler
            .When(HttpMethod.Get, "/api/v1/namespaces/test/services")
            .Respond(
                HttpStatusCode.OK,
                new StringContent(
                    "{\"apiVersion\":\"v1\",\"items\":[{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":" +
                    "{\"labels\":{\"label1\":\"value1\"},\"name\":\"endpoint1\",\"namespace\":\"test\",\"uid\":" +
                    "\"uids1\"}},{\"apiVersion\":\"v1\",\"kind\":\"Service\",\"metadata\":{\"labels\":{\"label2\":" +
                    "\"value2\"},\"name\":\"endpoint2\",\"namespace\":\"test\",\"uid\":\"uids2\"}}]," +
                    "\"kind\":\"List\",\"metadata\"" +
                    ":{\"resourceVersion\":\"\",\"selfLink\":\"\"}}"));

        using var client = new k8s.Kubernetes(
            config: new KubernetesClientConfiguration { Host = "http://localhost" },
            httpClient: mockHttpMessageHandler.ToHttpClient());

        var k8sOptions = new KubernetesDiscoveryOptions() { Enabled = false, Namespace = "test" };
        var options = new TestOptionsMonitor<KubernetesDiscoveryOptions>(k8sOptions);

        var discoveryClient = new KubernetesDiscoveryClient(
            new DefaultIsServicePortSecureResolver(options.CurrentValue),
            client,
            options);

        var services = discoveryClient.Services;

        Assert.NotNull(services);
        Assert.Empty(services);

        // turn it on
        k8sOptions.Enabled = true;

        services = discoveryClient.Services;

        Assert.NotNull(services);
        Assert.Equal(actual: services.Count, expected: 2);
        Assert.True(services.Contains("endpoint1"));
        Assert.True(services.Contains("endpoint2"));
    }
}