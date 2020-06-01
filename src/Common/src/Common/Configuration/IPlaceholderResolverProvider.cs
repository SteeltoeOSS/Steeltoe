// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Common.Configuration
{
    public interface IPlaceholderResolverProvider : IConfigurationProvider
    {
        public IList<IConfigurationProvider> Providers { get; }

        public IList<string> ResolvedKeys { get; }
    }
}
