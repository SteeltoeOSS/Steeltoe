// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Steeltoe;

public static partial class TestHelpers
{
    public static ILoggerFactory GetLoggerFactory()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));

#pragma warning disable CS0618 // Type or member is obsolete
        serviceCollection.AddLogging(builder => builder.AddConsole(opts => opts.DisableColors = true));
#pragma warning restore CS0618 // Type or member is obsolete

        serviceCollection.AddLogging(builder => builder.AddDebug());
        return serviceCollection.BuildServiceProvider().GetService<ILoggerFactory>();
    }
}
#endif
