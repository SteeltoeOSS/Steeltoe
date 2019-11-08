using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Configuration
{
    public interface IPlaceholderResolverProvider : IConfigurationProvider
    {
        public IList<IConfigurationProvider> Providers { get; }

        public IList<string> ResolvedKeys { get; }
    }
}
