// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test
{
    public class EnvEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            IEnvOptions options = null;
            IConfiguration configuration = null;
        #if NETCOREAPP3_0
            IHostEnvironment env = null;
        #else
            IHostingEnvironment env = null;
        #endif

            Assert.Throws<ArgumentNullException>(() => new EnvEndpoint(options, configuration, env));

            options = new EnvEndpointOptions();
            Assert.Throws<ArgumentNullException>(() => new EnvEndpoint(options, configuration, env));

            configuration = new ConfigurationBuilder().Build();
            Assert.Throws<ArgumentNullException>(() => new EnvEndpoint(options, configuration, env));
        }

        [Fact]
        public void GetPropertySourceName_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            var builder = new ConfigurationBuilder();
            builder.AddEnvironmentVariables();
            var config = builder.Build();
            var env = HostingHelpers.GetHostingEnvironment();
            var ep = new EnvEndpoint(opts, config, env);

            var provider = config.Providers.Single();
            var name = ep.GetPropertySourceName(provider);
            Assert.Equal(provider.GetType().Name, name);

            builder = new ConfigurationBuilder();
            builder.AddJsonFile("foobar", true);
            config = builder.Build();

            ep = new EnvEndpoint(opts, config, env);

            provider = config.Providers.Single();
            name = ep.GetPropertySourceName(provider);
            Assert.Equal("JsonConfigurationProvider: [foobar]", name);
        }

        [Fact]
        public void GetPropertySourceDescriptor_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
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

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddInMemoryCollection(otherAppsettings);
            var config = configurationBuilder.Build();
            var env = HostingHelpers.GetHostingEnvironment();
            var ep = new EnvEndpoint(opts, config, env);

            var appsettingsProvider = config.Providers.ElementAt(0);
            var appsettingsDesc = ep.GetPropertySourceDescriptor(appsettingsProvider);

            var otherAppsettingsProvider = config.Providers.ElementAt(1);
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

        [Fact]
        public void GetPropertySources_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "false",
                ["management:endpoints:heapdump:enabled"] = "true",
                ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var env = HostingHelpers.GetHostingEnvironment();
            var ep = new EnvEndpoint(opts, config, env);
            var result = ep.GetPropertySources(config);
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

        [Fact]
        public void GetPropertySources_ReturnsExpected_WithPlaceholders()
        {
            // arrange
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["appsManagerBase"] = "${management:endpoints:path}"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).AddPlaceholderResolver().Build();
            var endpoint = new EnvEndpoint(new EnvEndpointOptions(), config, HostingHelpers.GetHostingEnvironment());

            // act
            var result = endpoint.GetPropertySources(config);
            var testProp = config["appsManagerBase"];

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.NotNull(testProp);
            Assert.Equal("/cloudfoundryapplication", testProp);
        }

        [Fact]
        public void DoInvoke_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "false",
                ["management:endpoints:heapdump:enabled"] = "true",
                ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var env = HostingHelpers.GetHostingEnvironment();
            var ep = new EnvEndpoint(opts, config, env);
            var result = ep.DoInvoke(config);
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

        [Fact]
        public void Sanitized_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            var appsettings = new Dictionary<string, string>()
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
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var env = HostingHelpers.GetHostingEnvironment();
            var ep = new EnvEndpoint(opts, config, env);
            var result = ep.DoInvoke(config);
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

        [Fact]
        public void Sanitized_NonDefault_WhenSet()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:env:keystosanitize:0"] = "credentials",
                ["password"] = "mysecret"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var opts = new EnvEndpointOptions(config);
            var env = HostingHelpers.GetHostingEnvironment();
            var ep = new EnvEndpoint(opts, config, env);
            var result = ep.DoInvoke(config);
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
