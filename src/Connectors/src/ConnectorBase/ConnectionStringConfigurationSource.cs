// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Connector;

public class ConnectionStringConfigurationSource : IConfigurationSource
{
    internal IList<IConfigurationSource> Sources;

    public ConnectionStringConfigurationSource(IList<IConfigurationSource> sources)
    {
        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        this.Sources = new List<IConfigurationSource>(sources);
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new ConnectionStringConfigurationProvider(Sources.Select(s => s.Build(builder)));
}
