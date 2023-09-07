// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connectors.RabbitMQ;
using Xunit;

namespace Steeltoe.Connectors.Test.RabbitMQ;

public sealed class RabbitMQConnectorTests
{
    private const string MultiVcapServicesJson = @"{
  ""p.rabbitmq"": [
    {
      ""label"": ""p.rabbitmq"",
      ""provider"": null,
      ""plan"": ""single-node"",
      ""name"": ""myRabbitMQServiceOne"",
      ""tags"": [
        ""rabbitmq""
      ],
      ""instance_guid"": ""377d9d72-e951-4a1c-82e8-99c3c4933368"",
      ""instance_name"": ""myRabbitMQServiceOne"",
      ""binding_guid"": ""d2fd2c9d-ef84-406b-8401-f2ffacaafda6"",
      ""binding_name"": null,
      ""credentials"": {
        ""dashboard_url"": ""https://rmq-377d9d72-e951-4a1c-82e8-99c3c4933368.sys.benicia.cf-app.com"",
        ""hostname"": ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh"",
        ""hostnames"": [
          ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh""
        ],
        ""http_api_uri"": ""https://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@rmq-377d9d72-e951-4a1c-82e8-99c3c4933368.sys.benicia.cf-app.com/api/"",
        ""http_api_uris"": [
          ""https://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@rmq-377d9d72-e951-4a1c-82e8-99c3c4933368.sys.benicia.cf-app.com/api/""
        ],
        ""password"": ""AqntL6IwehKOGssE51psrJYd"",
        ""protocols"": {
          ""amqp"": {
            ""host"": ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh"",
            ""hosts"": [
              ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh""
            ],
            ""password"": ""AqntL6IwehKOGssE51psrJYd"",
            ""port"": 5672,
            ""ssl"": false,
            ""uri"": ""amqp://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh/377d9d72-e951-4a1c-82e8-99c3c4933368"",
            ""uris"": [
              ""amqp://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh/377d9d72-e951-4a1c-82e8-99c3c4933368""
            ],
            ""username"": ""d2fd2c9d-ef84-406b-8401-f2ffacaafda6"",
            ""vhost"": ""377d9d72-e951-4a1c-82e8-99c3c4933368""
          }
        },
        ""ssl"": false,
        ""uri"": ""amqp://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh/377d9d72-e951-4a1c-82e8-99c3c4933368"",
        ""uris"": [
          ""amqp://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh/377d9d72-e951-4a1c-82e8-99c3c4933368""
        ],
        ""username"": ""d2fd2c9d-ef84-406b-8401-f2ffacaafda6"",
        ""vhost"": ""377d9d72-e951-4a1c-82e8-99c3c4933368""
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    },
    {
      ""label"": ""p.rabbitmq"",
      ""provider"": null,
      ""plan"": ""single-node"",
      ""name"": ""myRabbitMQServiceTwo"",
      ""tags"": [
        ""rabbitmq""
      ],
      ""instance_guid"": ""eda94023-757e-4ef4-9315-dcba2e96efb5"",
      ""instance_name"": ""myRabbitMQServiceTwo"",
      ""binding_guid"": ""799815ea-9f6d-40e3-9317-7cc8ca43552f"",
      ""binding_name"": null,
      ""credentials"": {
        ""dashboard_url"": ""https://rmq-eda94023-757e-4ef4-9315-dcba2e96efb5.sys.benicia.cf-app.com"",
        ""hostname"": ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh"",
        ""hostnames"": [
          ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh""
        ],
        ""http_api_uri"": ""https://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@rmq-eda94023-757e-4ef4-9315-dcba2e96efb5.sys.benicia.cf-app.com/api/"",
        ""http_api_uris"": [
          ""https://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@rmq-eda94023-757e-4ef4-9315-dcba2e96efb5.sys.benicia.cf-app.com/api/""
        ],
        ""password"": ""mw2cCEufc9biidCBA_lYILxc"",
        ""protocols"": {
          ""amqp"": {
            ""host"": ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh"",
            ""hosts"": [
              ""q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh""
            ],
            ""password"": ""mw2cCEufc9biidCBA_lYILxc"",
            ""port"": 5672,
            ""ssl"": false,
            ""uri"": ""amqp://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh/eda94023-757e-4ef4-9315-dcba2e96efb5"",
            ""uris"": [
              ""amqp://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh/eda94023-757e-4ef4-9315-dcba2e96efb5""
            ],
            ""username"": ""799815ea-9f6d-40e3-9317-7cc8ca43552f"",
            ""vhost"": ""eda94023-757e-4ef4-9315-dcba2e96efb5""
          }
        },
        ""ssl"": false,
        ""uri"": ""amqp://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh/eda94023-757e-4ef4-9315-dcba2e96efb5"",
        ""uris"": [
          ""amqp://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh/eda94023-757e-4ef4-9315-dcba2e96efb5""
        ],
        ""username"": ""799815ea-9f6d-40e3-9317-7cc8ca43552f"",
        ""vhost"": ""eda94023-757e-4ef4-9315-dcba2e96efb5""
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    }
  ]
}";

    private const string SingleVcapServicesJson = @"{
  ""p.rabbitmq"": [
    {
      ""label"": ""p.rabbitmq"",
      ""provider"": null,
      ""plan"": ""single-node"",
      ""name"": ""myRabbitMQService"",
      ""tags"": [
        ""rabbitmq""
      ],
      ""instance_guid"": ""e73ca795-53a8-4ba1-9a38-45424ad28248"",
      ""instance_name"": ""myRabbitMQService"",
      ""binding_guid"": ""0a1ad792-8937-4770-8868-542f5f14126f"",
      ""binding_name"": null,
      ""credentials"": {
        ""dashboard_url"": ""https://rmq-e73ca795-53a8-4ba1-9a38-45424ad28248.sys.cotati.cf-app.com"",
        ""hostname"": ""q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh"",
        ""hostnames"": [
          ""q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh""
        ],
        ""http_api_uri"": ""https://0a1ad792-8937-4770-8868-542f5f14126f:b2Npj1K_eZOrD-fyoewg46rA@rmq-e73ca795-53a8-4ba1-9a38-45424ad28248.sys.cotati.cf-app.com/api/"",
        ""http_api_uris"": [
          ""https://0a1ad792-8937-4770-8868-542f5f14126f:b2Npj1K_eZOrD-fyoewg46rA@rmq-e73ca795-53a8-4ba1-9a38-45424ad28248.sys.cotati.cf-app.com/api/""
        ],
        ""password"": ""b2Npj1K_eZOrD-fyoewg46rA"",
        ""protocols"": {
          ""amqp"": {
            ""host"": ""q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh"",
            ""hosts"": [
              ""q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh""
            ],
            ""password"": ""b2Npj1K_eZOrD-fyoewg46rA"",
            ""port"": 5672,
            ""ssl"": false,
            ""uri"": ""amqp://0a1ad792-8937-4770-8868-542f5f14126f:b2Npj1K_eZOrD-fyoewg46rA@q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh/e73ca795-53a8-4ba1-9a38-45424ad28248"",
            ""uris"": [
              ""amqp://0a1ad792-8937-4770-8868-542f5f14126f:b2Npj1K_eZOrD-fyoewg46rA@q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh/e73ca795-53a8-4ba1-9a38-45424ad28248""
            ],
            ""username"": ""0a1ad792-8937-4770-8868-542f5f14126f"",
            ""vhost"": ""e73ca795-53a8-4ba1-9a38-45424ad28248""
          }
        },
        ""ssl"": false,
        ""uri"": ""amqp://0a1ad792-8937-4770-8868-542f5f14126f:b2Npj1K_eZOrD-fyoewg46rA@q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh/e73ca795-53a8-4ba1-9a38-45424ad28248"",
        ""uris"": [
          ""amqp://0a1ad792-8937-4770-8868-542f5f14126f:b2Npj1K_eZOrD-fyoewg46rA@q-s0.rabbitmq-server.cotati-services-subnet.service-instance-e73ca795-53a8-4ba1-9a38-45424ad28248.bosh/e73ca795-53a8-4ba1-9a38-45424ad28248""
        ],
        ""username"": ""0a1ad792-8937-4770-8868-542f5f14126f"",
        ""vhost"": ""e73ca795-53a8-4ba1-9a38-45424ad28248""
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    }
  ]
}";

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceOne:ConnectionString"] = "amqp://user1:pass1@host1:5672/virtual-host-1",
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceTwo:ConnectionString"] = "amqps://user2:pass2@host2:5672/virtual-host-2"
        });

        builder.AddRabbitMQ();

        await using WebApplication app = builder.Build();
        var optionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<RabbitMQOptions>>();

        RabbitMQOptions optionsOne = optionsSnapshot.Get("myRabbitMQServiceOne");
        optionsOne.ConnectionString.Should().Be("amqp://user1:pass1@host1:5672/virtual-host-1");

        RabbitMQOptions optionsTwo = optionsSnapshot.Get("myRabbitMQServiceTwo");
        optionsTwo.ConnectionString.Should().Be("amqps://user2:pass2@host2:5672/virtual-host-2");
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceOne:ConnectionString"] = "amqps://user:pass@localhost:5672"
        });

        builder.AddRabbitMQ();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();

        RabbitMQOptions optionsOne = optionsMonitor.Get("myRabbitMQServiceOne");

        optionsOne.ConnectionString.Should().Be(
            "amqp://d2fd2c9d-ef84-406b-8401-f2ffacaafda6:AqntL6IwehKOGssE51psrJYd@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-377d9d72-e951-4a1c-82e8-99c3c4933368.bosh:5672/377d9d72-e951-4a1c-82e8-99c3c4933368");

        RabbitMQOptions optionsTwo = optionsMonitor.Get("myRabbitMQServiceTwo");

        optionsTwo.ConnectionString.Should().Be(
            "amqp://799815ea-9f6d-40e3-9317-7cc8ca43552f:mw2cCEufc9biidCBA_lYILxc@q-s0.rabbitmq-server.benicia-services-subnet.service-instance-eda94023-757e-4ef4-9315-dcba2e96efb5.bosh:5672/eda94023-757e-4ef4-9315-dcba2e96efb5");
    }

    [Fact]
    public async Task Binds_options_with_Kubernetes_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeDirectory("db");
        fileProvider.IncludeFile("db/provider", "bitnami");
        fileProvider.IncludeFile("db/type", "rabbitmq");
        fileProvider.IncludeFile("db/host", "10.0.98.152");
        fileProvider.IncludeFile("db/port", "5672");
        fileProvider.IncludeFile("db/username", "rabbitmq");
        fileProvider.IncludeFile("db/password", "PZ3kQK91dAYpRte0a9gGmCWYED3ijI0R");

        var reader = new KubernetesMemoryServiceBindingsReader(fileProvider);
        builder.Configuration.AddKubernetesServiceBindings(false, true, _ => false, reader);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:RabbitMQ:db:ConnectionString"] = "amqps://user:pass@localhost:5672/extra-virtual-host"
        });

        builder.AddRabbitMQ();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();

        RabbitMQOptions dbOptions = optionsMonitor.Get("db");

        dbOptions.ConnectionString.Should().Be("amqp://rabbitmq:PZ3kQK91dAYpRte0a9gGmCWYED3ijI0R@10.0.98.152:5672/");
    }

    [Fact]
    public async Task Registers_ConnectorFactory()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceOne:ConnectionString"] = "amqp://user1:pass1@host1:5672/virtual-host-1",
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceTwo:ConnectionString"] = "amqps://user2:pass2@host2:5672/virtual-host-2"
        });

        builder.AddRabbitMQ(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
                RabbitMQOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnection(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RabbitMQOptions, IConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myRabbitMQServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myRabbitMQServiceTwo");

        var connectionOne = (FakeConnection)connectorFactory.Get("myRabbitMQServiceOne").GetConnection();
        connectionOne.ConnectionString.Should().Be("amqp://user1:pass1@host1:5672/virtual-host-1");

        var connectionTwo = (FakeConnection)connectorFactory.Get("myRabbitMQServiceTwo").GetConnection();
        connectionTwo.ConnectionString.Should().Be("amqps://user2:pass2@host2:5672/virtual-host-2");

        IConnection connectionOneAgain = connectorFactory.Get("myRabbitMQServiceOne").GetConnection();
        connectionOneAgain.Should().BeSameAs(connectionOne);
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceOne:ConnectionString"] = "amqp://user1:pass1@host1:5672/virtual-host-1",
            ["Steeltoe:Client:RabbitMQ:myRabbitMQServiceTwo:ConnectionString"] = "amqps://user2:pass2@host2:5672/virtual-host-2"
        });

        builder.AddRabbitMQ(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
                RabbitMQOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnection(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();
        RabbitMQHealthContributor[] rabbitMQHealthContributors = healthContributors.Should().AllBeOfType<RabbitMQHealthContributor>().Subject.ToArray();
        rabbitMQHealthContributors.Should().HaveCount(2);

        rabbitMQHealthContributors[0].Id.Should().Be("RabbitMQ");
        rabbitMQHealthContributors[0].ServiceName.Should().Be("myRabbitMQServiceOne");
        rabbitMQHealthContributors[0].Host.Should().Be("host1");

        rabbitMQHealthContributors[1].Id.Should().Be("RabbitMQ");
        rabbitMQHealthContributors[1].ServiceName.Should().Be("myRabbitMQServiceTwo");
        rabbitMQHealthContributors[1].Host.Should().Be("host2");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddRabbitMQ(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
                RabbitMQOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnection(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RabbitMQOptions, IConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);
        connectorFactory.ServiceBindingNames.Should().Contain("myRabbitMQService");

        RabbitMQOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();

        RabbitMQOptions namedOptions = connectorFactory.Get("myRabbitMQService").Options;
        namedOptions.ConnectionString.Should().Be(defaultOptions.ConnectionString);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:RabbitMQ:Default:ConnectionString"] = "amqp://localhost:5672/my-virtual-host"
        });

        builder.AddRabbitMQ(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
                RabbitMQOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnection(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RabbitMQOptions, IConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);

        RabbitMQOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_no_bindings_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddRabbitMQ(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
                RabbitMQOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnection(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RabbitMQOptions, IConnection>>();

        connectorFactory.ServiceBindingNames.Should().BeEmpty();

        RabbitMQOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().BeNull();

        var connection = (FakeConnection)connectorFactory.Get().GetConnection();
        connection.ConnectionString.Should().BeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    private sealed class FakeConnection : IConnection
    {
        public string? ConnectionString { get; }

        public int LocalPort => throw new NotImplementedException();
        public int RemotePort => throw new NotImplementedException();
        public ushort ChannelMax => throw new NotImplementedException();
        public IDictionary<string, object> ClientProperties => throw new NotImplementedException();
        public ShutdownEventArgs CloseReason => throw new NotImplementedException();
        public AmqpTcpEndpoint Endpoint => throw new NotImplementedException();
        public uint FrameMax => throw new NotImplementedException();
        public TimeSpan Heartbeat => throw new NotImplementedException();
        public bool IsOpen => throw new NotImplementedException();
        public AmqpTcpEndpoint[] KnownHosts => throw new NotImplementedException();
        public IProtocol Protocol => throw new NotImplementedException();
        public IDictionary<string, object> ServerProperties => throw new NotImplementedException();
        public IList<ShutdownReportEntry> ShutdownReport => throw new NotImplementedException();
        public string ClientProvidedName => throw new NotImplementedException();

        public event EventHandler<CallbackExceptionEventArgs> CallbackException
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<ConnectionBlockedEventArgs> ConnectionBlocked
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<ShutdownEventArgs> ConnectionShutdown
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<EventArgs> ConnectionUnblocked
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public FakeConnection(string? connectionString)
        {
            ConnectionString = connectionString;
        }

        public void Dispose()
        {
        }

        public void UpdateSecret(string newSecret, string reason)
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText)
        {
            throw new NotImplementedException();
        }

        public void Abort(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Close(ushort reasonCode, string reasonText)
        {
            throw new NotImplementedException();
        }

        public void Close(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Close(ushort reasonCode, string reasonText, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public IModel CreateModel()
        {
            throw new NotImplementedException();
        }

        public void HandleConnectionBlocked(string reason)
        {
            throw new NotImplementedException();
        }

        public void HandleConnectionUnblocked()
        {
            throw new NotImplementedException();
        }
    }
}
