// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Management.Endpoint.Environment;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Environment;

public sealed class EnvironmentEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public EnvironmentEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        IOptionsMonitor<EnvironmentEndpointOptions> options = GetOptionsMonitorFromSettings<EnvironmentEndpointOptions>();
        IConfiguration configuration = null;
        const IHostEnvironment environment = null;

        Assert.Throws<ArgumentNullException>(() => new EnvironmentEndpointHandler(options, configuration, environment, NullLoggerFactory.Instance));

        configuration = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentNullException>(() => new EnvironmentEndpointHandler(options, configuration, environment, NullLoggerFactory.Instance));
    }

    [Fact]
    public void GetPropertySourceName_ReturnsExpected()
    {
        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, _) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvironmentActuatorServices();
            };

            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddEnvironmentVariables();
            };

            var ep = (EnvironmentEndpointHandler)tc.GetRequiredService<IEnvironmentEndpointHandler>();

            IConfigurationProvider provider = tc.Configuration.Providers.Single();
            string name = ep.GetPropertySourceName(provider);
            Assert.Equal(provider.GetType().Name, name);
        }

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, _) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvironmentActuatorServices();
            };

            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddJsonFile("foobar", true);
            };

            var ep = (EnvironmentEndpointHandler)tc.GetRequiredService<IEnvironmentEndpointHandler>();

            IConfigurationProvider provider = tc.Configuration.Providers.Single();
            string name = ep.GetPropertySourceName(provider);
            Assert.Equal("JsonConfigurationProvider: [foobar]", name);
        }
    }

    [Fact]
    public void GetPropertySourceDescriptor_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:heapdump:sensitive"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true",
            ["common"] = "appsettings",
            ["CharSize"] = "should not duplicate"
        };

        var otherAppsettings = new Dictionary<string, string>
        {
            ["common"] = "otherAppsettings",
            ["charSize"] = "should not duplicate"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddEnvironmentActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
            configuration.AddInMemoryCollection(otherAppsettings);
        };

        var ep = (EnvironmentEndpointHandler)tc.GetRequiredService<IEnvironmentEndpointHandler>();

        IConfigurationProvider appsettingsProvider = tc.Configuration.Providers.ElementAt(0);
        PropertySourceDescriptor appsettingsDesc = ep.GetPropertySourceDescriptor(appsettingsProvider);

        IConfigurationProvider otherAppsettingsProvider = tc.Configuration.Providers.ElementAt(1);
        PropertySourceDescriptor otherAppsettingsDesc = ep.GetPropertySourceDescriptor(otherAppsettingsProvider);

        Assert.Equal("MemoryConfigurationProvider", appsettingsDesc.Name);
        IDictionary<string, PropertyValueDescriptor> props = appsettingsDesc.Properties;
        Assert.NotNull(props);
        Assert.Equal(9, props.Count);
        Assert.Contains("management:endpoints:enabled", props.Keys);
        PropertyValueDescriptor prop = props["management:endpoints:enabled"];
        Assert.NotNull(prop);
        Assert.Equal("false", prop.Value);
        Assert.Null(prop.Origin);

        IDictionary<string, PropertyValueDescriptor> otherProps = otherAppsettingsDesc.Properties;
        PropertyValueDescriptor appSettingsCommonProp = props["common"];
        PropertyValueDescriptor otherAppSettingCommonProp = otherProps["common"];
        Assert.Equal("appsettings", appSettingsCommonProp.Value);
        Assert.Equal("otherAppsettings", otherAppSettingCommonProp.Value);
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddEnvironmentActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = (EnvironmentEndpointHandler)tc.GetRequiredService<IEnvironmentEndpointHandler>();
        IList<PropertySourceDescriptor> result = ep.GetPropertySources(tc.Configuration);
        Assert.NotNull(result);
        Assert.Single(result);

        PropertySourceDescriptor desc = result[0];

        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);
        Assert.Equal(6, props.Count);
        Assert.Contains("management:endpoints:cloudfoundry:validatecertificates", props.Keys);
        PropertyValueDescriptor prop = props["management:endpoints:cloudfoundry:validatecertificates"];
        Assert.NotNull(prop);
        Assert.Equal("true", prop.Value);
        Assert.Null(prop.Origin);
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected_WithPlaceholders()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["appsManagerBase"] = "${management:endpoints:path}"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddEnvironmentActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
            configuration.AddPlaceholderResolver();
        };

        var endpoint = (EnvironmentEndpointHandler)tc.GetRequiredService<IEnvironmentEndpointHandler>();

        IList<PropertySourceDescriptor> result = endpoint.GetPropertySources(tc.Configuration);
        string testProp = tc.Configuration["appsManagerBase"];

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.NotNull(testProp);
        Assert.Equal("/cloudfoundryapplication", testProp);
    }

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddEnvironmentActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = tc.GetRequiredService<IEnvironmentEndpointHandler>();
        EnvironmentResponse result = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Single(result.ActiveProfiles);
        Assert.Equal("EnvironmentName", result.ActiveProfiles[0]);
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
        var appsettings = new Dictionary<string, string>
        {
            ["password"] = "mysecret",
            ["secret"] = "mysecret",
            ["key"] = "mysecret",
            ["token"] = "mysecret",
            ["my_credentials"] = "mysecret",
            ["credentials_of"] = "mysecret",
            ["my_credentialsof"] = "mysecret",
            ["vcap_services"] = "mysecret"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddEnvironmentActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = tc.GetRequiredService<IEnvironmentEndpointHandler>();
        EnvironmentResponse result = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);

        PropertySourceDescriptor desc = result.PropertySources[0];

        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);

        foreach (string key in appsettings.Keys)
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
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:env:keystosanitize:0"] = "credentials",
            ["password"] = "mysecret"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddEnvironmentActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = tc.GetRequiredService<IEnvironmentEndpointHandler>();
        EnvironmentResponse result = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);

        PropertySourceDescriptor desc = result.PropertySources[0];
        Assert.Equal("MemoryConfigurationProvider", desc.Name);
        IDictionary<string, PropertyValueDescriptor> props = desc.Properties;
        Assert.NotNull(props);
        Assert.Contains("password", props.Keys);
        Assert.NotNull(props["password"]);
        Assert.Equal("mysecret", props["password"].Value);
    }
}
