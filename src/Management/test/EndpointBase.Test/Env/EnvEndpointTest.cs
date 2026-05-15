// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Env.Test;

public class EnvEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public EnvEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        IEnvOptions options = null;
        IConfiguration configuration = null;
        IHostEnvironment env = null;

        Assert.Throws<ArgumentNullException>(() => new EnvEndpoint(options, configuration, env));

        options = new EnvEndpointOptions();
        Assert.Throws<ArgumentNullException>(() => new EnvEndpoint(options, configuration, env));

        configuration = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentNullException>(() => new EnvEndpoint(options, configuration, env));
    }

    [Fact]
    public void GetPropertySourceName_ReturnsExpected()
    {
        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddEnvironmentVariables();
            };

            var ep = tc.GetService<IEnvEndpoint>();

            var provider = tc.Configuration.Providers.Single();
            var name = ep.GetPropertySourceName(provider);
            Assert.Equal(provider.GetType().Name, name);
        }

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddJsonFile("foobar", true);
            };

            var ep = tc.GetService<IEnvEndpoint>();

            var provider = tc.Configuration.Providers.Single();
            var name = ep.GetPropertySourceName(provider);
            Assert.Equal("JsonConfigurationProvider: [foobar]", name);
        }
    }

    [Fact]
    public void GetPropertySourceDescriptor_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>()
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

        var otherAppsettings = new Dictionary<string, string>()
        {
            ["common"] = "otherAppsettings",
            ["charSize"] = "should not duplicate"
        };

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddInMemoryCollection(appsettings);
                configuration.AddInMemoryCollection(otherAppsettings);
            };

            var ep = tc.GetService<IEnvEndpoint>();

            var appsettingsProvider = tc.Configuration.Providers.ElementAt(0);
            var appsettingsDesc = ep.GetPropertySourceDescriptor(appsettingsProvider);

            var otherAppsettingsProvider = tc.Configuration.Providers.ElementAt(1);
            var otherAppsettingsDesc = ep.GetPropertySourceDescriptor(otherAppsettingsProvider);

            Assert.Equal("MemoryConfigurationProvider", appsettingsDesc.Name);
            var props = appsettingsDesc.Properties;
            Assert.NotNull(props);
            Assert.Equal(9, props.Count);
            Assert.Contains("management:endpoints:enabled", props.Keys);
            var prop = props["management:endpoints:enabled"];
            Assert.NotNull(prop);
            Assert.Equal("false", prop.Value);
            Assert.Null(prop.Origin);

            var otherProps = otherAppsettingsDesc.Properties;
            var appsettingsCommonProp = props["common"];
            var otherAppsettingCommonProp = otherProps["common"];
            Assert.Equal("appsettings", appsettingsCommonProp.Value);
            Assert.Equal("otherAppsettings", otherAppsettingCommonProp.Value);
        }
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddInMemoryCollection(appsettings);
            };

            var ep = tc.GetService<IEnvEndpoint>();
            var result = ep.GetPropertySources(tc.Configuration);
            Assert.NotNull(result);
            Assert.Single(result);

            var desc = result[0];

            Assert.Equal("MemoryConfigurationProvider", desc.Name);
            var props = desc.Properties;
            Assert.NotNull(props);
            Assert.Equal(6, props.Count);
            Assert.Contains("management:endpoints:cloudfoundry:validatecertificates", props.Keys);
            var prop = props["management:endpoints:cloudfoundry:validatecertificates"];
            Assert.NotNull(prop);
            Assert.Equal("true", prop.Value);
            Assert.Null(prop.Origin);
        }
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected_WithPlaceholders()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["appsManagerBase"] = "${management:endpoints:path}"
        };

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddInMemoryCollection(appsettings);
                configuration.AddPlaceholderResolver();
            };

            var endpoint = tc.GetService<IEnvEndpoint>();

            var result = endpoint.GetPropertySources(tc.Configuration);
            var testProp = tc.Configuration["appsManagerBase"];

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.NotNull(testProp);
            Assert.Equal("/cloudfoundryapplication", testProp);
        }
    }

    [Fact]
    public void DoInvoke_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddInMemoryCollection(appsettings);
            };

            var ep = tc.GetService<IEnvEndpoint>();
            var result = ep.Invoke();
            Assert.NotNull(result);
            Assert.Single(result.ActiveProfiles);
            Assert.Equal("EnvironmentName", result.ActiveProfiles[0]);
            Assert.Single(result.PropertySources);

            var desc = result.PropertySources[0];

            Assert.Equal("MemoryConfigurationProvider", desc.Name);
            var props = desc.Properties;
            Assert.NotNull(props);
            Assert.Equal(6, props.Count);
            Assert.Contains("management:endpoints:loggers:enabled", props.Keys);
            var prop = props["management:endpoints:loggers:enabled"];
            Assert.NotNull(prop);
            Assert.Equal("false", prop.Value);
            Assert.Null(prop.Origin);
        }
    }

    [Fact]
    public void Sanitized_ReturnsExpected_FullySanitized()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["password"] = null,
            ["password"] = string.Empty,
            ["password"] = "mysecret",
            ["secret"] = "mysecret",
            ["key"] = "mysecret",
            ["token"] = "mysecret",
            ["my_credentials"] = "jdbc:postgresql://:secret@127.0.0.1:5432/db",
            ["credentials_of"] = "mysecret",
            ["my_credentialsof"] = "amqp://user1:pass1@host1,amqps://user2:pass2@host2",
            ["vcap_services"] = "mysecret",
            ["my_connectionstring"] = "mysecret",
            ["connectionstring_of"] = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Pwd=myPassword;",
            ["ConnectionStrings:OrderDb"] = "Server=myServerAddress;Database=orders;User Id=myUsername;Password=myPassword;"
        };

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddInMemoryCollection(appsettings);
            };

            var ep = tc.GetService<IEnvEndpoint>();
            var result = ep.Invoke();
            Assert.NotNull(result);

            var desc = result.PropertySources[0];

            Assert.Equal("MemoryConfigurationProvider", desc.Name);
            var props = desc.Properties;
            Assert.NotNull(props);
            foreach (var key in appsettings.Keys)
            {
                Assert.Contains(key, props.Keys);
                Assert.NotNull(props[key]);
                Assert.Equal("******", props[key].Value);
                Assert.Null(props[key].Origin);
            }
        }
    }

    [Theory]
    [InlineData("someKey", "host=localhost;password=secret;port=1", "host=localhost;password=******;port=1")]
    [InlineData("someKey", "host=localhost;pwd=secret;port=1", "host=localhost;pwd=******;port=1")]
    [InlineData("someKey", "password=secret;host=localhost;port=1", "password=******;host=localhost;port=1")]
    [InlineData("someKey", "host=localhost;port=1;password=secret", "host=localhost;port=1;password=******")]
    [InlineData("someKey", "host=localhost;notapassword=secret;port=1", "host=localhost;notapassword=secret;port=1")]
    [InlineData("someKey", "PWD=secret;Password=other", "PWD=******;Password=******")]
    [InlineData("someKey", "password=secret", "password=******")]
    [InlineData("someKey", "host=1; password = abc ;port=2", "host=1; password = ******;port=2")]
    [InlineData("someKey", null, null)]
    [InlineData("someKey", "", "")]
    [InlineData("someKey", "host=localhost;notapassword=secret;password=real;port=1", "host=localhost;notapassword=secret;password=******;port=1")]
    [InlineData("someKey", "host=localhost;password=;port=1", "host=localhost;password=;port=1")]
    [InlineData("someKey", "password=;host=localhost", "password=;host=localhost")]
    [InlineData("someKey", "host=localhost;port=1;password=", "host=localhost;port=1;password=")]
    [InlineData("someKey", "host=localhost;pwd=;password=;port=1", "host=localhost;pwd=;password=;port=1")]
    [InlineData("someKey", "host=localhost;password=;password=secret;port=1", "host=localhost;password=;password=******;port=1")]
    [InlineData("db_url", "jdbc:postgresql://user:secret@127.0.0.1:5432/db", "jdbc:postgresql://user:******@127.0.0.1:5432/db")]
    [InlineData("db_url_empty_user", "jdbc:postgresql://:secret@127.0.0.1:5432/db", "jdbc:postgresql://:******@127.0.0.1:5432/db")]
    [InlineData("db_url_multiple", "amqp://user1:pass1@host1,amqps://user2:pass2@host2", "amqp://user1:******@host1,amqps://user2:******@host2")]
    [InlineData("db_connection", "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;", "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=******;")]
    [InlineData("db_connection_pwd", "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Pwd=myPassword;", "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Pwd=******;")]
    [InlineData("ConnectionStrings:OrderDb", "Server=myServerAddress;Database=orders;User Id=myUsername;Password=myPassword;", "******")]
    [InlineData("normal_url", "https://localhost:8080/api/v1", "https://localhost:8080/api/v1")]
    public void Sanitize_ReturnsExpected(string key, string input, string expected)
    {
        var sanitizer = new Sanitizer(new[] { "password", "secret", ".*connectionstring.*" });

        var result = sanitizer.Sanitize(new KeyValuePair<string, string>(key, input));

        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Sanitized_NonDefault_WhenSet()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["management:endpoints:env:keystosanitize:0"] = "credentials",
            ["password"] = "mysecret"
        };

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton(HostingHelpers.GetHostingEnvironment());
                services.AddEnvActuatorServices(configuration);
            };
            tc.AdditionalConfiguration = configuration =>
            {
                configuration.AddInMemoryCollection(appsettings);
            };

            var ep = tc.GetService<IEnvEndpoint>();
            var result = ep.Invoke();
            Assert.NotNull(result);

            var desc = result.PropertySources[0];
            Assert.Equal("MemoryConfigurationProvider", desc.Name);
            var props = desc.Properties;
            Assert.NotNull(props);
            Assert.Contains("password", props.Keys);
            Assert.NotNull(props["password"]);
            Assert.Equal("mysecret", props["password"].Value);
        }
    }
}