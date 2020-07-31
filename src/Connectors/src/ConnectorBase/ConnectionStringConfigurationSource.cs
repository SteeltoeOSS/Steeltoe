// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Connector
{
    public class ConnectionStringConfigurationSource : IConfigurationSource
    {
        internal IList<IConfigurationSource> _sources;

        public ConnectionStringConfigurationSource(IList<IConfigurationSource> sources)
        {
            _sources = new List<IConfigurationSource>(sources);
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var providers = new List<IConfigurationProvider>();
            foreach (var source in _sources)
            {
                var provider = source.Build(builder);
                providers.Add(provider);
            }

            return new ConnectionStringConfigurationProvider(providers);
        }
    }
}
