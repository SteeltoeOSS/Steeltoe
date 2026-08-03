// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Configuration.CloudFoundry.Test.ServiceBindings;

public sealed class ConfigurationBuilderExtensionsTest
{
    private const string VcapServicesJson = """
        {
          "elephantsql": [
            {
              "name": "elephantsql-c6c60",
              "label": "elephantsql",
              "tags": [
                "postgres",
                "postgresql",
                "relational"
              ],
              "plan": "turtle",
              "credentials": {
                "uri": "postgres://seilbmbd:ABcdEF@babar.elephantsql.com:5432/seilbmbd"
              }
            }
          ],
          "sendgrid": [
            {
              "name": "mysendgrid",
              "label": "sendgrid",
              "tags": [
                "smtp"
              ],
              "plan": "free",
              "credentials": {
                "hostname": "smtp.sendgrid.net",
                "username": "QvsXMbJ3rK",
                "password": "HCHMOYluTv"
              }
            }
          ]
        }
        """;

    [Fact]
    public void AddCloudFoundryServiceBindings_RegistersProcessors()
    {
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings();

        builder.Sources.Should().ContainSingle();
        CloudFoundryServiceBindingConfigurationSource source = builder.Sources[0].Should().BeOfType<CloudFoundryServiceBindingConfigurationSource>().Subject;
        source.PostProcessors.Should().NotBeEmpty();
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_RegistersSubsetOfProcessors()
    {
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.PostgreSql | CloudFoundryServiceBrokerTypes.MySql);

        builder.Sources.Should().ContainSingle();
        CloudFoundryServiceBindingConfigurationSource source = builder.Sources[0].Should().BeOfType<CloudFoundryServiceBindingConfigurationSource>().Subject;
        source.PostProcessors.Should().HaveCount(2);
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_DoesNotAddMultipleSourcesForSamePostProcessor()
    {
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.None);
        builder.AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.PostgreSql | CloudFoundryServiceBrokerTypes.MySql);
        builder.AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.PostgreSql | CloudFoundryServiceBrokerTypes.SqlServer);
        builder.AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.MySql | CloudFoundryServiceBrokerTypes.RabbitMQ);
        builder.AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.SqlServer | CloudFoundryServiceBrokerTypes.RabbitMQ);

        CloudFoundryServiceBindingConfigurationSource[] sources = [.. builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>()];

        sources.Should().HaveCount(3);
        sources[0].BrokerTypes.Should().Be(CloudFoundryServiceBrokerTypes.PostgreSql | CloudFoundryServiceBrokerTypes.MySql);
        sources[1].BrokerTypes.Should().Be(CloudFoundryServiceBrokerTypes.SqlServer);
        sources[2].BrokerTypes.Should().Be(CloudFoundryServiceBrokerTypes.RabbitMQ);
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_EnvironmentVariableSet_LoadsServiceBindings()
    {
        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", VcapServicesJson);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings();
        IConfigurationRoot configurationRoot = builder.Build();

        configurationRoot.GetValue<string>("vcap:services:elephantsql:0:name").Should().Be("elephantsql-c6c60");
        configurationRoot.GetValue<string>("vcap:services:sendgrid:0:name").Should().Be("mysendgrid");
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_EnvironmentVariableNotSet_DoesNotThrow()
    {
        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", null);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings();

        Action action = () => builder.Build();
        action.Should().NotThrow();
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_CanIgnoreKey()
    {
        Predicate<string> ignoreKeyPredicate = key => key == "vcap:services:sendgrid:0:name";

        var reader = new StringServiceBindingsReader(VcapServicesJson);
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings(ignoreKeyPredicate, reader, NullLoggerFactory.Instance);
        IConfigurationRoot configurationRoot = builder.Build();

        configurationRoot.GetValue<string>("vcap:services:elephantsql:0:name").Should().Be("elephantsql-c6c60");
        configurationRoot.GetValue<string>("vcap:services:sendgrid:0:name").Should().BeNull();
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_CredHub_LoadsFullBindingWithDeepStructure()
    {
        const string credHubJson = """
            {
              "credhub": [
                {
                  "name": "my-credhub-service",
                  "label": "credhub",
                  "tags": [
                    "credhub"
                  ],
                  "plan": "default",
                  "credentials": {
                    "Encrypt__Key": "secret-value",
                    "some.setting": "setting-value",
                    "app": {
                      "database": {
                        "connection_string": "Server=tcp:sql.domain;Database=db;",
                        "max_pool_size": "20"
                      },
                      "servers": [
                        "server1.example.com",
                        "server2.example.com"
                      ]
                    },
                    "endpoints": [
                      "https://api1.example.com",
                      "https://api2.example.com"
                    ],
                    "[maps.google.com]": "maps-api-key"
                  }
                }
              ]
            }
            """;

        var reader = new StringServiceBindingsReader(credHubJson);
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings(reader);
        IConfigurationRoot configurationRoot = builder.Build();

        const string keyPrefix = "steeltoe:service-bindings:credhub:my-credhub-service:";
        configurationRoot.GetValue<string>($"{keyPrefix}Encrypt__Key").Should().BeNull();

        // "__" is a .NET-only environment variable convention and is left untouched here.
        configurationRoot.GetValue<string>("Encrypt__Key").Should().Be("secret-value");

        // Dots are converted to colons, so secrets can be shared between Spring and .NET apps.
        configurationRoot.GetValue<string>("some:setting").Should().Be("setting-value");

        configurationRoot.GetValue<string>("app:database:connection_string").Should().Be("Server=tcp:sql.domain;Database=db;");
        configurationRoot.GetValue<string>("app:database:max_pool_size").Should().Be("20");
        configurationRoot.GetValue<string>("app:servers:0").Should().Be("server1.example.com");
        configurationRoot.GetValue<string>("app:servers:1").Should().Be("server2.example.com");
        configurationRoot.GetValue<string>("endpoints:0").Should().Be("https://api1.example.com");
        configurationRoot.GetValue<string>("endpoints:1").Should().Be("https://api2.example.com");

        // A bracket-wrapped segment escapes its dots, so they are kept literal instead of being converted to colons.
        configurationRoot.GetValue<string>("maps.google.com").Should().Be("maps-api-key");
    }
}
