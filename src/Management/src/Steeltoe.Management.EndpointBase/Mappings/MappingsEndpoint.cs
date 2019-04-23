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

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class MappingsEndpoint : AbstractEndpoint<ApplicationMappings>
    {
        private readonly ILogger<MappingsEndpoint> _logger;

        public MappingsEndpoint(IMappingsOptions options, ILogger<MappingsEndpoint> logger = null)
            : base(options)
        {
            _logger = logger;
        }

        public new IMappingsOptions Options
        {
            get
            {
                return options as IMappingsOptions;
            }
        }

        public override ApplicationMappings Invoke()
        {
            // Note: This is not called, as all the work in
            // done in runtime specific code (i.e. Asp.NET, Asp.NET Core, etc)
            return null;
        }
    }
}
