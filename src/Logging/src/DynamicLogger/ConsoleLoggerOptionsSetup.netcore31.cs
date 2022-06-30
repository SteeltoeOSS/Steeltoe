// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Logging;

internal sealed class ConsoleLoggerOptionsSetup : ConfigureFromConfigurationOptions<ConsoleLoggerOptions>
{
    public ConsoleLoggerOptionsSetup(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
        : base(providerConfiguration.Configuration)
    {
    }

    public override void Configure(ConsoleLoggerOptions options)
    {
        if (Platform.IsCloudFoundry)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            options.DisableColors = true;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        base.Configure(options);
    }
}
#endif
