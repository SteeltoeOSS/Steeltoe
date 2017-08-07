
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Info
{
    public class InfoEndpoint : AbstractEndpoint<Dictionary<string, object>>
    {
        private IList<IInfoContributor> _contributors;
        private ILogger<InfoEndpoint> _logger;

        public new IInfoOptions Options
        {
            get
            {
                return options as IInfoOptions;
            }
        }

        public InfoEndpoint(IInfoOptions options, IEnumerable<IInfoContributor> contributors, ILogger<InfoEndpoint> logger) :
            base(options)
        {
            _logger = logger;
            _contributors = contributors.ToList();
        }

        public override Dictionary<string, object> Invoke()
        {
            return BuildInfo(_contributors);
        }

        protected virtual Dictionary<string, object> BuildInfo(IList<IInfoContributor> infoContributors)
        {
            IInfoBuilder builder = new InfoBuilder();
            foreach (var contributor in infoContributors)
            {
                try
                {
                    contributor.Contribute(builder);
                } catch (Exception)
                {
                    // Log
                }
            }

            return builder.Build();
        }

    }
}
