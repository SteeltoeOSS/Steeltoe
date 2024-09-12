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

        builder.Sources.Should().HaveCount(1);
        CloudFoundryServiceBindingConfigurationSource source = builder.Sources[0].Should().BeOfType<CloudFoundryServiceBindingConfigurationSource>().Subject;
        source.PostProcessors.Should().NotBeEmpty();
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
}
