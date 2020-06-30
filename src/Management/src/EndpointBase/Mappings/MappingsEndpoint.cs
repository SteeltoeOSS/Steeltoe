// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
