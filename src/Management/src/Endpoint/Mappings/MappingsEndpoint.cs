// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Mappings;

public class MappingsEndpoint : IEndpoint<ApplicationMappings>
{
    //public new IMappingsOptions Options => options as IMappingsOptions;

    public MappingsEndpoint(IOptionsMonitor<MappingsEndpointOptions> options, ILogger<MappingsEndpoint> logger = null)
       // : base(options)
    {
        Options = options;
    }

    public IOptionsMonitor<MappingsEndpointOptions> Options { get; }

    IEndpointOptions IEndpoint.Options => Options.CurrentValue;

    public ApplicationMappings Invoke()
    {
        // Note: This is not called, as all the work in
        // done in runtime specific code (i.e. Asp.NET, Asp.NET Core, etc)
        return null;
    }
}
