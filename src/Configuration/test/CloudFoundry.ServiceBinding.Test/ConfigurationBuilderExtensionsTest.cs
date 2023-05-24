// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.Test;

public sealed class ConfigurationBuilderExtensionsTest
{
    private const string VcapServicesJson = @"
    {
        ""elephantsql"": [{
            ""name"": ""elephantsql-c6c60"",
            ""label"": ""elephantsql"",
            ""tags"": [
                ""postgres"",
                ""postgresql"",
                ""relational""
            ],
            ""plan"": ""turtle"",
            ""credentials"": {""uri"": ""postgres://seilbmbd:ABcdEF@babar.elephantsql.com:5432/seilbmbd""}
        }],
        ""sendgrid"": [{
            ""name"": ""mysendgrid"",
            ""label"": ""sendgrid"",
            ""tags"": [""smtp""],
            ""plan"": ""free"",
            ""credentials"": {
                ""hostname"": ""smtp.sendgrid.net"",
                ""username"": ""QvsXMbJ3rK"",
                ""password"": ""HCHMOYluTv""
            }
        }]
    }";

    [Fact]
    public void AddCloudFoundryServiceBindings_ThrowsOnNulls()
    {
        var builder = new ConfigurationBuilder();
        var reader = new StringServiceBindingsReader(string.Empty);

        Action action1 = () => ((ConfigurationBuilder)null).AddCloudFoundryServiceBindings();
        action1.Should().ThrowExactly<ArgumentNullException>().WithParameterName("builder");

        Action action2 = () => builder.AddCloudFoundryServiceBindings((Predicate<string>)null);
        action2.Should().ThrowExactly<ArgumentNullException>().WithParameterName("ignoreKeyPredicate");

        Action action3 = () => builder.AddCloudFoundryServiceBindings((IServiceBindingsReader)null);
        action3.Should().ThrowExactly<ArgumentNullException>().WithParameterName("serviceBindingsReader");

        Action action4 = () => builder.AddCloudFoundryServiceBindings(null, reader);
        action4.Should().ThrowExactly<ArgumentNullException>().WithParameterName("ignoreKeyPredicate");

        Action action5 = () => builder.AddCloudFoundryServiceBindings(_ => false, null);
        action5.Should().ThrowExactly<ArgumentNullException>().WithParameterName("serviceBindingsReader");
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_RegistersNoProcessors()
    {
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundryServiceBindings();

        builder.Sources.Should().HaveCount(1);
        CloudFoundryServiceBindingConfigurationSource source = builder.Sources[0].Should().BeOfType<CloudFoundryServiceBindingConfigurationSource>().Subject;
        source.PostProcessors.Should().BeEmpty();
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_EnvironmentVariableSet_LoadsServiceBindings()
    {
        Environment.SetEnvironmentVariable("VCAP_SERVICES", VcapServicesJson);

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundryServiceBindings();
            IConfigurationRoot configurationRoot = builder.Build();

            configurationRoot.GetValue<string>("vcap:services:elephantsql:0:name").Should().Be("elephantsql-c6c60");
            configurationRoot.GetValue<string>("vcap:services:sendgrid:0:name").Should().Be("mysendgrid");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }
    }

    [Fact]
    public void AddCloudFoundryServiceBindings_EnvironmentVariableNotSet_DoesNotThrow()
    {
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
        builder.AddCloudFoundryServiceBindings(ignoreKeyPredicate, reader);
        IConfigurationRoot configurationRoot = builder.Build();

        configurationRoot.GetValue<string>("vcap:services:elephantsql:0:name").Should().Be("elephantsql-c6c60");
        configurationRoot.GetValue<string>("vcap:services:sendgrid:0:name").Should().BeNull();
    }
}
