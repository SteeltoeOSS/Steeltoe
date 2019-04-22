// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        public InfoEndpoint(IInfoOptions options, IEnumerable<IInfoContributor> contributors, ILogger<InfoEndpoint> logger = null)
            : base(options)
        {
            _logger = logger;
            _contributors = contributors.ToList();
        }

        public new IInfoOptions Options
        {
            get
            {
                return options as IInfoOptions;
            }
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
                }
                catch (Exception e)
                {
                    _logger?.LogError("Exception: {0}", e);
                }
            }

            return builder.Build();
        }
    }
}
