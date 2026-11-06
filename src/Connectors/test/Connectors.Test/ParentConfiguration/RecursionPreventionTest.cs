// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Connectors.Test.ParentConfiguration;

/// <summary>
/// Verifies that building the temporary copy of the parent configuration terminates when the connector source is wrapped in composite sources or
/// contained in the nested configuration of Config Server, which both build the sources they contain again. The counting source is part of every build,
/// so that endless rebuilding fails the test instead of overflowing the stack.
/// </summary>
public sealed class RecursionPreventionTest
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Builds_parent_configuration_when_connector_source_is_wrapped_in_composite_source(bool usePlaceholderResolver, bool useDecryption)
    {
        // Building the parent configuration rebuilds the composite source, which contains the connector source again. Without skipping the connector
        // source there, this would never end.
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.AddSilentConnector();
        builder.AddCompositeSources(usePlaceholderResolver, useDecryption);

        using (builder.BuildAsRoot())
        {
            // One build for the actual configuration, and one for the temporary copy of the parent configuration used to post-process connection strings.
            countingSource.Providers.Should().HaveCount(2);
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }

    [Fact]
    public void Connector_source_is_skipped_in_Config_Server_nested_configuration_to_prevent_infinite_recursion()
    {
        // Loading the connector builds the parent configuration, which contains Config Server, which builds a nested configuration from the same sources.
        // Without skipping the connector source there, this would never end.
        var countingSource = new DisposeCountingConfigurationSource();

        var builder = new ConfigurationBuilder();
        builder.Add(countingSource);
        builder.AddSilentConnector();
        builder.AddSilentConfigServer();

        using (ConfigurationRoot configurationRoot = builder.BuildAsRoot())
        {
            // Only the actual configuration contains a connector provider.
            configurationRoot.Providers.OfType<ConnectionStringPostProcessorConfigurationProvider>().Should().ContainSingle();
        }

        countingSource.Providers.Should().OnlyContain(provider => provider.DisposeCount == 1);
    }
}
