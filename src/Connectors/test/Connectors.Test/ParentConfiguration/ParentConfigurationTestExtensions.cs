// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Connectors.PostgreSql;

namespace Steeltoe.Connectors.Test.ParentConfiguration;

internal static class ParentConfigurationTestExtensions
{
    /// <summary>
    /// Adds a connector, which builds a temporary copy of the parent configuration to post-process connection strings.
    /// </summary>
    public static void AddSilentConnector(this IConfigurationBuilder builder, bool detectConfigurationChanges = false)
    {
        builder.ConfigurePostgreSql(options =>
        {
            options.SkipDefaultServiceBindings = true;
            options.DetectConfigurationChanges = detectConfigurationChanges;
        });
    }

    /// <summary>
    /// Adds Config Server without contacting it, which still builds a nested configuration from the sources that were added before it.
    /// </summary>
    public static void AddSilentConfigServer(this IConfigurationBuilder builder)
    {
        builder.AddConfigServer(new ConfigServerClientOptions
        {
            Enabled = false
        });
    }

    /// <summary>
    /// Adds the placeholder resolver and/or decryption, which move all sources that were added before them into composite sources that build them.
    /// </summary>
    public static void AddCompositeSources(this IConfigurationBuilder builder, bool usePlaceholderResolver, bool useDecryption)
    {
        if (usePlaceholderResolver)
        {
            builder.AddPlaceholderResolver();
        }

        if (useDecryption)
        {
            builder.AddDecryption();
        }
    }
}
