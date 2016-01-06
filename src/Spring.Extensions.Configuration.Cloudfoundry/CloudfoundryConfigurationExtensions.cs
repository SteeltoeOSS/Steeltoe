using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spring.Extensions.Configuration.Cloudfoundry
{
    public static class CloudfoundryConfigurationExtensions
    {

        public static IConfigurationBuilder AddCloudfoundry(this IConfigurationBuilder configurationBuilder)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            configurationBuilder.Add(new CloudfoundryConfigurationProvider());

            return configurationBuilder;

        }
    }
}
