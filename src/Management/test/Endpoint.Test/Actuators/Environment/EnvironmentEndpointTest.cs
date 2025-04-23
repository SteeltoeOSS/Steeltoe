// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Management.Endpoint.Actuators.Environment;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class EnvironmentEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public void GetPropertySourceName_ReturnsExpected()
    {
        using (var testContext = new SteeltoeTestContext(_testOutputHelper))
        {
            testContext.AdditionalServices = (services, _) =>
            {
                services.AddSingleton(TestHostEnvironmentFactory.Create());
                services.AddEnvironmentActuator();
            };

            testContext.AdditionalConfiguration = configuration =>
            {
                configuration.AddEnvironmentVariables();
            };

            var handler = (EnvironmentEndpointHandler)testContext.GetRequiredService<IEnvironmentEndpointHandler>();

            IConfigurationProvider provider = testContext.Configuration.Providers.Single();
            string name = handler.GetPropertySourceName(provider);
            Assert.Equal(provider.GetType().Name, name);
        }

        using (var testContext = new SteeltoeTestContext(_testOutputHelper))
        {
            testContext.AdditionalServices = (services, _) =>
            {
                services.AddSingleton(TestHostEnvironmentFactory.Create());
                services.AddEnvironmentActuator();
            };

            testContext.AdditionalConfiguration = configuration =>
            {
                configuration.AddJsonFile("foobar", true);
            };

            var handler = (EnvironmentEndpointHandler)testContext.GetRequiredService<IEnvironmentEndpointHandler>();

            IConfigurationProvider provider = testContext.Configuration.Providers.Single();
            string name = handler.GetPropertySourceName(provider);
            Assert.Equal("JsonConfigurationProvider: [foobar]", name);
        }
    }

    [Fact]
    public void GetPropertySourceDescriptor_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapDump:enabled"] = "true",
            ["management:endpoints:heapDump:sensitive"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true",
            ["common"] = "appSettings",
            ["CharSize"] = "should not duplicate"
        };

        var otherAppSettings = new Dictionary<string, string?>
        {
            ["common"] = "otherAppSettings",
            ["charSize"] = "should not duplicate"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.AddEnvironmentActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
            configuration.AddInMemoryCollection(otherAppSettings);
        };

        var handler = (EnvironmentEndpointHandler)testContext.GetRequiredService<IEnvironmentEndpointHandler>();

        IConfigurationProvider appSettingsProvider = testContext.Configuration.Providers.ElementAt(0);
        PropertySourceDescriptor descriptor = handler.GetPropertySourceDescriptor(appSettingsProvider);

        IConfigurationProvider otherAppSettingsProvider = testContext.Configuration.Providers.ElementAt(1);
        PropertySourceDescriptor otherDescriptor = handler.GetPropertySourceDescriptor(otherAppSettingsProvider);

        Assert.Equal("MemoryConfigurationProvider", descriptor.Name);
        IDictionary<string, PropertyValueDescriptor> props = descriptor.Properties;
        Assert.NotNull(props);
        Assert.Equal(9, props.Count);
        Assert.Contains("management:endpoints:enabled", props.Keys);
        PropertyValueDescriptor prop = props["management:endpoints:enabled"];
        Assert.NotNull(prop);
        Assert.Equal("false", prop.Value);
        Assert.Null(prop.Origin);

        IDictionary<string, PropertyValueDescriptor> otherProps = otherDescriptor.Properties;
        PropertyValueDescriptor appSettingsCommonProp = props["common"];
        PropertyValueDescriptor otherAppSettingCommonProp = otherProps["common"];
        Assert.Equal("appSettings", appSettingsCommonProp.Value);
        Assert.Equal("otherAppSettings", otherAppSettingCommonProp.Value);
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapDump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.AddEnvironmentActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        var handler = (EnvironmentEndpointHandler)testContext.GetRequiredService<IEnvironmentEndpointHandler>();
        IList<PropertySourceDescriptor> result = handler.GetPropertySources();
        Assert.NotNull(result);
        Assert.Single(result);

        PropertySourceDescriptor desc = result[0];

        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);
        Assert.Equal(6, props.Count);
        Assert.Contains("management:endpoints:cloudfoundry:validateCertificates", props.Keys);
        PropertyValueDescriptor prop = props["management:endpoints:cloudfoundry:validateCertificates"];
        Assert.NotNull(prop);
        Assert.Equal("true", prop.Value);
        Assert.Null(prop.Origin);
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected_WithPlaceholders()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["appsManagerBase"] = "${management:endpoints:path}"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.AddEnvironmentActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
            configuration.AddPlaceholderResolver();
        };

        var endpoint = (EnvironmentEndpointHandler)testContext.GetRequiredService<IEnvironmentEndpointHandler>();

        IList<PropertySourceDescriptor> result = endpoint.GetPropertySources();
        string? testProp = testContext.Configuration["appsManagerBase"];

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.NotNull(testProp);
        Assert.Equal("/cloudfoundryapplication", testProp);
    }

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapDump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.AddEnvironmentActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        var handler = testContext.GetRequiredService<IEnvironmentEndpointHandler>();
        EnvironmentResponse result = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Single(result.ActiveProfiles);
        Assert.Equal("Test", result.ActiveProfiles[0]);
        Assert.Single(result.PropertySources);

        PropertySourceDescriptor desc = result.PropertySources[0];

        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);
        Assert.Equal(6, props.Count);
        Assert.Contains("management:endpoints:loggers:enabled", props.Keys);
        PropertyValueDescriptor prop = props["management:endpoints:loggers:enabled"];
        Assert.NotNull(prop);
        Assert.Equal("false", prop.Value);
        Assert.Null(prop.Origin);
    }

    [Fact]
    public async Task Sanitized_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["password"] = "my-secret",
            ["secret"] = "my-secret",
            ["key"] = "my-secret",
            ["example-key"] = "my-secret",
            ["token"] = "my-secret",
            ["my_credentials"] = "my-secret",
            ["credentials_of"] = "my-secret",
            ["my_credentialsOf"] = "my-secret",
            ["vcap_services"] = "my-secret"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.AddEnvironmentActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        var handler = testContext.GetRequiredService<IEnvironmentEndpointHandler>();
        EnvironmentResponse result = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        PropertySourceDescriptor desc = result.PropertySources[0];

        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);

        foreach (string key in appSettings.Keys)
        {
            Assert.Contains(key, props.Keys);
            Assert.NotNull(props[key]);
            Assert.Equal("******", props[key].Value);
            Assert.Null(props[key].Origin);
        }
    }

    [Fact]
    public async Task Sanitized_NonDefault_WhenSet()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:env:keysToSanitize:0"] = "credentials",
            ["password"] = "my-secret"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.AddEnvironmentActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        var handler = testContext.GetRequiredService<IEnvironmentEndpointHandler>();
        EnvironmentResponse result = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        PropertySourceDescriptor desc = result.PropertySources[0];
        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);
        Assert.Contains("password", props.Keys);
        Assert.NotNull(props["password"]);
        Assert.Equal("my-secret", props["password"].Value);
    }
}
