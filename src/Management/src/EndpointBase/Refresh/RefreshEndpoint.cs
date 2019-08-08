// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Refresh
{
    public class RefreshEndpoint : AbstractEndpoint<IList<string>>
    {
        private readonly ILogger<RefreshEndpoint> _logger;
        private readonly IConfiguration _configuration;

        public RefreshEndpoint(IRefreshOptions options, IConfiguration configuration, ILogger<RefreshEndpoint> logger = null)
            : base(options)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        public new IRefreshOptions Options
        {
            get
            {
                return options as IRefreshOptions;
            }
        }

        public override IList<string> Invoke()
        {
            return DoInvoke(_configuration);
        }

        public IList<string> DoInvoke(IConfiguration configuration)
        {
            if (configuration is IConfigurationRoot root)
            {
                root.Reload();
            }

            List<string> keys = new List<string>();
            foreach (var kvp in configuration.AsEnumerable())
            {
                keys.Add(kvp.Key);
            }

            return keys;
        }
    }
}
